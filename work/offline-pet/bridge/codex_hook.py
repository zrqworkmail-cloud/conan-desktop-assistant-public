"""Forward only lifecycle metadata. Never store prompts, tool input, or responses."""
import json,sys,uuid,os
from pathlib import Path
from datetime import datetime,timezone

def session_title(event):
    title=event.get('session_title') or event.get('thread_title') or event.get('title')
    if not isinstance(title,str) or not title.strip():
        index=Path(__file__).resolve().parent.parent/'session_index.jsonl'
        title=''
        if index.is_file() and not index.is_symlink() and index.stat().st_size<=8388608:
            with index.open(encoding='utf-8-sig') as stream:
                for line in stream:
                    if len(line)>16384:continue
                    try:row=json.loads(line)
                    except ValueError:continue
                    if isinstance(row,dict) and row.get('id')==event.get('session_id') and isinstance(row.get('thread_name'),str):title=row['thread_name']
    return ''.join(c for c in title if ord(c)>=32 and ord(c)!=127).strip()[:120]

def main():
    try:
        event=json.load(sys.stdin)
        allowed={'SessionStart','UserPromptSubmit','PreToolUse','PostToolUse','PermissionRequest','Stop','Interrupt','SessionEnd'}
        name=event.get('hook_event_name')
        if name not in allowed:return
        config=json.loads((Path(__file__).parent/'bridge-config.json').read_text(encoding='utf-8-sig'))
        target=Path(os.environ.get('CONAN_PET_EVENT_DIR',config['eventsLinux']))
        target.mkdir(parents=True,exist_ok=True)
        try:title=session_title(event)
        except (OSError,ValueError,TypeError):title=''
        data={'protocol':'conan-event-v1','event':name,'session':str(event.get('session_id',''))[:128],'utc':datetime.now(timezone.utc).isoformat(),'title':title}
        if not data['session']:return
        dest=target/('conan-event-'+uuid.uuid4().hex+'.json');temp=dest.with_suffix('.tmp')
        temp.write_text(json.dumps(data),encoding='utf-8');temp.replace(dest)
    except Exception:
        pass # Companion failures must not affect Codex execution.

if __name__=='__main__':
    main()
    print('{}')

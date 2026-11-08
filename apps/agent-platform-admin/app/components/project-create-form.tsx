import type { FormEvent } from "react";

export function ProjectCreateForm({ name, isSaving, onNameChange, onSubmit, onCancel }: { name: string; isSaving: boolean; onNameChange: (value: string) => void; onSubmit: (event: FormEvent<HTMLFormElement>) => void; onCancel: () => void }) {
  return <form className="project-form project-create-form" onSubmit={onSubmit}><label htmlFor="project-name">New project name</label><div><input id="project-name" value={name} autoFocus maxLength={200} onChange={(event) => onNameChange(event.target.value)} required /><button className="new-button" disabled={isSaving}>Create project</button><button className="text-button" type="button" onClick={onCancel}>Cancel</button></div></form>;
}

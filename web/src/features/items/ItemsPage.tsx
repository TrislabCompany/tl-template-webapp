import { useEffect, useState, type FormEvent } from "react";
import { createItem, deleteItem, listItems, updateItem, type Item } from "./api";

// The sample feature: a list of items, a form that adds one, and a form per
// item that renames it. Replace it with the product's own features.
export default function ItemsPage() {
  const [items, setItems] = useState<Item[]>();
  const [error, setError] = useState<string>();
  const [name, setName] = useState("");

  useEffect(() => {
    listItems().then(setItems, (e: Error) => setError(e.message));
  }, []);

  const run = async (action: () => Promise<void>) => {
    setError(undefined);
    try {
      await action();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const add = (event: FormEvent) => {
    event.preventDefault();
    run(async () => {
      const item = await createItem(name.trim());
      setItems((current) => [...(current ?? []), item]);
      setName("");
    });
  };

  const save = (item: Item) =>
    run(async () => {
      const saved = await updateItem(item);
      setItems((current) => current?.map((i) => (i.id === saved.id ? saved : i)));
    });

  const remove = (id: number) =>
    run(async () => {
      await deleteItem(id);
      setItems((current) => current?.filter((i) => i.id !== id));
    });

  return (
    <>
      <h1>Items</h1>
      {error && <p role="alert">{error}</p>}
      <form onSubmit={add} className="add">
        <label htmlFor="new-item">New item</label>
        <input id="new-item" value={name} onChange={(e) => setName(e.target.value)} maxLength={200} required />
        <button type="submit" disabled={!name.trim()}>
          Add
        </button>
      </form>
      {items === undefined ? (
        !error && <p>Loading…</p>
      ) : items.length === 0 ? (
        <p>No items yet.</p>
      ) : (
        <ul className="items">
          {items.map((item) => (
            <ItemRow key={item.id} item={item} onSave={save} onDelete={remove} />
          ))}
        </ul>
      )}
    </>
  );
}

function ItemRow({
  item,
  onSave,
  onDelete,
}: {
  item: Item;
  onSave: (item: Item) => Promise<void>;
  onDelete: (id: number) => Promise<void>;
}) {
  const [name, setName] = useState(item.name);

  const submit = (event: FormEvent) => {
    event.preventDefault();
    onSave({ ...item, name: name.trim() });
  };

  return (
    <li>
      <input
        type="checkbox"
        aria-label={`${item.name} done`}
        checked={item.done}
        onChange={(e) => onSave({ ...item, done: e.target.checked })}
      />
      <form onSubmit={submit}>
        <input aria-label={`Name of ${item.name}`} value={name} onChange={(e) => setName(e.target.value)} maxLength={200} required />
        <button type="submit" disabled={!name.trim() || name.trim() === item.name}>
          Save
        </button>
      </form>
      <button type="button" onClick={() => onDelete(item.id)}>
        Delete
      </button>
    </li>
  );
}

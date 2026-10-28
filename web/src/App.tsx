import { Link, Route, Routes } from "react-router";
import ItemsPage from "./features/items/ItemsPage";
import type { AppConfig } from "./lib/config";

export default function App({ config }: { config: AppConfig }) {
  return (
    <>
      <header>
        <Link to="/" className="brand">
          tl-template-webapp
        </Link>
        <span className="environment">{config.PLATFORM_ENVIRONMENT ?? "local"}</span>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<ItemsPage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
    </>
  );
}

function NotFound() {
  return (
    <>
      <h1>Page not found</h1>
      <p>
        <Link to="/">Back to the items</Link>
      </p>
    </>
  );
}

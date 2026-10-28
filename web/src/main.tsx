import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router";
import App from "./App";
import { loadConfig } from "./lib/config";
import "./styles.css";

// Loads /config.json first, so every part of the app sees the same values.
loadConfig().then((config) => {
  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <BrowserRouter>
        <App config={config} />
      </BrowserRouter>
    </StrictMode>,
  );
});

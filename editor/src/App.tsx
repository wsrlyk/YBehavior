import { useEffect, useState } from "react";
import "./App.css";
import { MainWindow } from "./windows/MainWindow";
import { TerminalWindow } from "./windows/TerminalWindow";
import { NotificationBubble } from "./components/NotificationBubble";
import { useDebugStore } from "./stores/debugStore";
import { useGlobalKeyboard } from "./hooks/useGlobalKeyboard";

function App() {
  const [route, setRoute] = useState(window.location.hash);
  const debugInit = useDebugStore((state) => state.init);
  const debugCleanup = useDebugStore((state) => state.cleanup);

  // Initialize debug store event listeners
  useEffect(() => {
    debugInit();
    return () => debugCleanup();
  }, [debugInit, debugCleanup]);

  // Global keyboard shortcuts
  useGlobalKeyboard();

  useEffect(() => {
    const handleHashChange = () => {
      setRoute(window.location.hash);
    };

    const handleContext = (e: MouseEvent) => e.preventDefault();
    window.addEventListener("contextmenu", handleContext);

    return () => {
      window.removeEventListener("hashchange", handleHashChange);
      window.removeEventListener("contextmenu", handleContext);
    };
  }, []);

  return (
    <>
      {route === "#/terminal" ? <TerminalWindow /> : <MainWindow />}
      <NotificationBubble />
    </>
  );
}

export default App;

import { useEffect, useState } from "react";
import "./App.css";
import { MainWindow } from "./windows/MainWindow";
import { TerminalWindow } from "./windows/TerminalWindow";
import { NotificationBubble } from "./components/NotificationBubble";

function App() {
  const [route, setRoute] = useState(window.location.hash);

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

"use client";

import { useEffect, useState } from "react";

const navigation = [
  { label: "Overview", icon: "grid" },
  { label: "Projects", icon: "folder" },
  { label: "Agents", icon: "sparkle" },
  { label: "MCP servers", icon: "server" },
  { label: "Instances", icon: "layers" },
  { label: "Event streams", icon: "pulse" },
] as const;

function Icon({ name }: { name: (typeof navigation)[number]["icon"] | "menu" | "close" }) {
  const common = { "aria-hidden": true, viewBox: "0 0 24 24", fill: "none", stroke: "currentColor", strokeWidth: 1.8 };

  if (name === "menu") return <svg {...common}><path d="M4 7h16M4 12h16M4 17h16" /></svg>;
  if (name === "close") return <svg {...common}><path d="m6 6 12 12M18 6 6 18" /></svg>;
  if (name === "grid") return <svg {...common}><rect x="4" y="4" width="6" height="6" rx="1" /><rect x="14" y="4" width="6" height="6" rx="1" /><rect x="4" y="14" width="6" height="6" rx="1" /><rect x="14" y="14" width="6" height="6" rx="1" /></svg>;
  if (name === "folder") return <svg {...common}><path d="M3 7.5A2.5 2.5 0 0 1 5.5 5H10l2 2h6.5A2.5 2.5 0 0 1 21 9.5v8a2.5 2.5 0 0 1-2.5 2.5h-13A2.5 2.5 0 0 1 3 17.5Z" /></svg>;
  if (name === "sparkle") return <svg {...common}><path d="m12 3 1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8ZM19 16l.8 2.2L22 19l-2.2.8L19 22l-.8-2.2L16 19l2.2-.8Z" /></svg>;
  if (name === "server") return <svg {...common}><rect x="3" y="4" width="18" height="6" rx="1.5" /><rect x="3" y="14" width="18" height="6" rx="1.5" /><path d="M7 7h.01M7 17h.01M11 7h6M11 17h6" /></svg>;
  if (name === "layers") return <svg {...common}><path d="m12 3 8 4.5-8 4.5-8-4.5L12 3Z" /><path d="m4 12 8 4.5 8-4.5M4 16.5l8 4.5 8-4.5" /></svg>;
  return <svg {...common}><path d="M4 15h3l2-6 4 10 2-4h5" /><path d="M3 4h18" /></svg>;
}

export default function HomePage() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isCollapsed, setIsCollapsed] = useState(false);

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") setIsMenuOpen(false);
    };

    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, []);

  const closeMenu = () => setIsMenuOpen(false);

  return (
    <div className={`app-shell ${isMenuOpen ? "menu-open" : ""} ${isCollapsed ? "menu-collapsed" : ""}`}>
      <button className="menu-backdrop" aria-label="Close navigation menu" onClick={closeMenu} />
      <aside className="sidebar" aria-label="Primary navigation">
        <div className="sidebar-header">
          <a className="brand" href="#overview" onClick={closeMenu} aria-label="Agent Platform overview">
            <span className="brand-mark">AP</span>
            <span className="brand-name">Agent Platform</span>
          </a>
          <button className="icon-button desktop-toggle" onClick={() => setIsCollapsed((value) => !value)} aria-label={isCollapsed ? "Expand navigation" : "Collapse navigation"}>
            <span className="collapse-chevron" aria-hidden="true">‹</span>
          </button>
          <button className="icon-button mobile-close" onClick={closeMenu} aria-label="Close navigation"><Icon name="close" /></button>
        </div>

        <nav>
          <p className="nav-label">Workspace</p>
          <ul>
            {navigation.map((item, index) => (
              <li key={item.label}>
                <a className={index === 0 ? "active" : ""} href={index === 0 ? "#overview" : `#${item.label.toLowerCase().replaceAll(" ", "-")}`} onClick={closeMenu}>
                  <span className="nav-icon"><Icon name={item.icon} /></span>
                  <span className="nav-text">{item.label}</span>
                </a>
              </li>
            ))}
          </ul>
        </nav>

        <div className="sidebar-footer">
          <span className="avatar" aria-hidden="true">OP</span>
          <span className="profile-copy"><strong>Operator</strong><small>Administrator</small></span>
        </div>
      </aside>

      <div className="workspace">
        <header className="topbar">
          <button className="icon-button mobile-menu" onClick={() => setIsMenuOpen(true)} aria-label="Open navigation" aria-expanded={isMenuOpen}><Icon name="menu" /></button>
          <div className="breadcrumb"><span>Workspace</span><span aria-hidden="true">/</span><strong>Overview</strong></div>
          <button className="new-button" type="button">New project</button>
        </header>

        <main id="overview">
          <div className="page-heading">
            <div>
              <p className="eyebrow">Agent Platform</p>
              <h1>Good afternoon, operator.</h1>
              <p>Manage projects, agent definitions, tool access, and active instances from one workspace.</p>
            </div>
            <button className="heading-action" type="button">View activity <span aria-hidden="true">→</span></button>
          </div>

          <section className="resource-grid" aria-label="Platform resources">
            {navigation.slice(1).map((item) => (
              <article key={item.label} id={item.label.toLowerCase().replaceAll(" ", "-")}>
                <span className="resource-icon"><Icon name={item.icon} /></span>
                <div><h2>{item.label}</h2><p>Coming soon</p></div>
                <span className="card-arrow" aria-hidden="true">→</span>
              </article>
            ))}
          </section>
        </main>
      </div>
    </div>
  );
}

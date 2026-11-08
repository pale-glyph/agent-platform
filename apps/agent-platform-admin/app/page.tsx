const sections = ["Projects", "Agents", "MCP servers", "Instances", "Event streams"];

export default function HomePage() {
  return (
    <main>
      <p className="eyebrow">Agent Platform</p>
      <h1>Administration</h1>
      <p className="intro">Manage projects, agent definitions, tool access, and running instances.</p>
      <section aria-label="Platform resources">
        {sections.map((section) => (
          <article key={section}>
            <h2>{section}</h2>
            <p>Coming soon</p>
          </article>
        ))}
      </section>
    </main>
  );
}

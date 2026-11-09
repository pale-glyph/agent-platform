"use client";

import { AdminApp } from "./components/layout/admin-app";
import { WorkspaceProvider } from "./providers/workspace-provider";

export default function HomePage() {
  return (
    <WorkspaceProvider>
      <AdminApp />
    </WorkspaceProvider>
  );
}

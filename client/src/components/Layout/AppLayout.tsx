import type { ReactNode } from 'react';

interface AppLayoutProps {
    sidebar: ReactNode;
    main: ReactNode;
}

export default function AppLayout({ sidebar, main }: AppLayoutProps) {
    return (
        <div className="flex h-screen bg-gray-900 text-white">
        <aside className="w-72 bg-gray-950 border-r border-gray-800 flex flex-col">
            {sidebar}
        </aside>
        <main className="flex-1 flex flex-col">
            {main}
        </main>
        </div>
    );
}
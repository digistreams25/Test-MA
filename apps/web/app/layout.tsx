import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'TwinFlux — Clash Resolver',
  description: 'BIM clash resolution platform for infrastructure projects',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className="dark">
      <body className="min-h-screen">
        {children}
      </body>
    </html>
  );
}

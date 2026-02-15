'use client';

import { useState } from 'react';
import Link from 'next/link';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface-0">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-brand-gold tracking-tight">
            TwinFlux
          </h1>
          <p className="text-gray-400 mt-2 text-sm">
            BIM Clash Resolution Platform
          </p>
        </div>
        <div className="card p-8">
          <h2 className="text-lg font-semibold text-gray-100 mb-6">Sign In</h2>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              window.location.href = '/dashboard';
            }}
            className="space-y-4"
          >
            <div>
              <label className="block text-sm text-gray-400 mb-1">Email</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="coordinator@example.com"
                className="input w-full"
              />
            </div>
            <div>
              <label className="block text-sm text-gray-400 mb-1">
                Password
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="input w-full"
              />
            </div>
            <button type="submit" className="btn-primary w-full mt-2">
              Sign In
            </button>
          </form>
          <p className="text-center text-sm text-gray-500 mt-4">
            Demo mode — click Sign In to enter
          </p>
        </div>
      </div>
    </div>
  );
}

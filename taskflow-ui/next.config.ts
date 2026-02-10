import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  // API proxy configuration
  async rewrites() {
    return [
      {
        source: '/api/v1/:path*',
        destination: `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5030/api/v1'}/:path*`,
      },
    ];
  },

  // Headers for CORS and security
  async headers() {
    return [
      {
        source: '/api/v1/:path*',
        headers: [
          { key: 'Access-Control-Allow-Credentials', value: 'true' },
          { key: 'Access-Control-Allow-Origin', value: '*' },
          { key: 'Access-Control-Allow-Methods', value: 'GET,DELETE,PATCH,POST,PUT' },
          { key: 'Access-Control-Allow-Headers', value: 'X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Content-Type, Date, X-Api-Version, Authorization' },
        ],
      },
    ];
  },

  // Turbopack configuration with CSS handling
  turbopack: {
    resolveAlias: {
      // Fix Tailwind CSS resolution
      'tailwindcss/plugin': 'tailwindcss/plugin.js',
    },
  },

  // React strict mode
  reactStrictMode: true,

  // TypeScript strict mode
  typescript: {
    ignoreBuildErrors: false,
  },

  // Image optimization config
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'localhost',
        port: '5001',
      },
    ],
  },

  // Experimental features
  experimental: {
    serverActions: {
      bodySizeLimit: '2mb',
    },
  },
};

export default nextConfig;
import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: 'https://localhost:5030/api/:path*',
      },
    ];
  },
  
  // Disable SSL verification in development (for self-signed certificates)
  webpack: (config, { dev, isServer }) => {
    if (dev && !isServer) {
      // Allow self-signed certificates in development
      process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
    }
    return config;
  },
  
  // Enable strict mode
  reactStrictMode: true,
  
  // Image optimization config (optional)
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'localhost',
        port: '5030',
      },
    ],
  },
};

export default nextConfig;
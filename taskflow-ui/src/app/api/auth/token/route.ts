import { cookies } from 'next/headers';
import { NextResponse } from 'next/server';

/**
 * GET /api/auth/token
 *
 * Server-side route that reads the httpOnly access_token cookie
 * (inaccessible to browser JS) and returns it so the SignalR client
 * can pass it as a query-string parameter (?access_token=...) as
 * required by the backend hub's OnMessageReceived handler.
 */
export async function GET() {
  const cookieStore = await cookies();
  const token = cookieStore.get('access_token')?.value;

  if (!token) {
    return NextResponse.json({ error: 'Not authenticated' }, { status: 401 });
  }

  return NextResponse.json({ token });
}

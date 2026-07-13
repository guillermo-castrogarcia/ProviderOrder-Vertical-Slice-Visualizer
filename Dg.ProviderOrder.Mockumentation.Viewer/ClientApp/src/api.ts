import type { Graph } from './types';

export async function fetchGraph(): Promise<Graph> {
  const res = await fetch('api/graph');
  if (!res.ok) {
    throw new Error(`Failed to load graph: ${res.status} ${res.statusText}`);
  }
  return (await res.json()) as Graph;
}

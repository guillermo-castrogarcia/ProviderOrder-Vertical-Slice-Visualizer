export type Side = 'Command' | 'Query';
export type AdapterType = 'Web' | 'NServiceBus' | 'Kafka' | 'Other';
/** Node colour category: a single adapter type, or "Mixed" for slices exposing several types. */
export type AdapterCategory = AdapterType | 'Mixed';

/** A free-standing incoming arrow: input into a slice from outside any known slice. */
export interface ExternalIncoming {
  adapterType: AdapterType;
  /** Name shown on hover — the message payload type, or the entry method for web adapters. */
  payload: string;
}

export interface GraphNode {
  id: string;
  label: string;
  side: Side;
  product: string;
  /** Input this slice receives from outside any known slice, one entry per external arrow. */
  externalIncoming: ExternalIncoming[];
  /** Colour category: the slice's single adapter type, or "Mixed" when it has several. */
  adapterCategory: AdapterCategory;
}

export interface GraphEdge {
  id: string;
  source: string;
  target: string;
  adapterType: AdapterType;
  kind: 'Messaging' | 'Web';
  /** Simple name of the payload travelling along the arrow — shown on hover. */
  payload: string;
  /** Commit-pinned GitHub URL to the payload type's source; null when the type has no known source. */
  sourceUrl?: string | null;
}

export interface Graph {
  nodes: GraphNode[];
  edges: GraphEdge[];
}

/** Data carried by a React Flow ellipse node. */
export interface SliceNodeData {
  label: string;
  side: Side;
  product: string;
  externalIncoming: ExternalIncoming[];
  adapterCategory: AdapterCategory;
  [key: string]: unknown;
}

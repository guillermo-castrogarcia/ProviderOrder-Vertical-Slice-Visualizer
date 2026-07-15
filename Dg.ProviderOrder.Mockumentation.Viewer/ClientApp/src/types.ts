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

/** A named thing with an optional commit-pinned GitHub source link. */
export interface Link {
  name: string;
  url?: string | null;
}

/** An incoming primary adapter: a Kafka consumer, NSB receiver, or controller action. */
export interface Adapter {
  adapterType: AdapterType;
  className: string;
  classUrl?: string | null;
  /** Entry method — the controller action / handler method. */
  entryMethod: string;
  entryMethodUrl?: string | null;
}

/** The handling service (class implementing the port) and the handler method it runs. */
export interface Handler {
  className: string;
  classUrl?: string | null;
  methodName: string;
  methodUrl?: string | null;
}

/** A single database column read or written by the slice. */
export interface DbAccess {
  database: string;
  schema: string;
  table: string;
  column: string;
}

/** The internals of a slice, shown when its node is expanded. */
export interface SliceDetail {
  adapters: Adapter[];
  handler?: Handler | null;
  /** The MediatR/Marinator request/command the handler receives; null for non-mediated ports. */
  mediatorPayload?: Link | null;
  reads: DbAccess[];
  writes: DbAccess[];
  nsbOut: Link[];
  kafkaOut: Link[];
  /** Outgoing web-service-client "{Controller}_{Action}" method names. */
  webRequests: string[];
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
  /** The slice's internals, rendered when the node is expanded. */
  detail: SliceDetail;
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
  detail: SliceDetail;
  /** True when the node is showing its expanded internals; injected per-render from App state. */
  expanded: boolean;
  /** Content-driven width + column widths for the expanded card; injected per-render from App state. */
  dims?: { width: number; height: number; leftW: number; rightW: number; readW: number; writeW: number };
  /** Collapses this node back to the ellipse; injected per-render from App state. */
  onCollapse: (id: string) => void;
  [key: string]: unknown;
}

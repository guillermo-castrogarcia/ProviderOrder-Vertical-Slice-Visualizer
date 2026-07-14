import { getBezierPath, type Edge, type EdgeProps } from '@xyflow/react';

export interface PayloadEdgeData {
  /** Simple name of the payload travelling along the arrow. */
  payload: string;
  /** Commit-pinned GitHub URL to the payload type's source; null when the type has no known source. */
  sourceUrl?: string | null;
  [key: string]: unknown;
}

export type PayloadEdgeType = Edge<PayloadEdgeData, 'payload'>;

/**
 * Default-looking edge that reveals the payload name in a native tooltip on hover. A wide, transparent
 * companion path enlarges the hover hit-area so the thin arrow is easy to point at. When the payload type
 * has a known source, the whole arrow is wrapped in an SVG link: the cursor turns into a pointer, the
 * browser status bar previews the destination, and a click opens the GitHub source in a new tab.
 */
export function PayloadEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  markerEnd,
  style,
  data,
}: EdgeProps<PayloadEdgeType>) {
  const [edgePath] = getBezierPath({
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  });
  const payload = data?.payload ?? '';
  const sourceUrl = data?.sourceUrl ?? null;
  // Linked arrows show the destination as the tooltip too, so the user can read where it points; plain
  // arrows keep the payload name.
  const tooltip = sourceUrl ? `${payload} → open source on GitHub` : payload;
  const cursor = sourceUrl ? 'pointer' : 'help';

  const paths = (
    <>
      <path d={edgePath} fill="none" stroke="transparent" strokeWidth={16} style={{ cursor }}>
        {payload && <title>{tooltip}</title>}
      </path>
      <path id={id} className="react-flow__edge-path" d={edgePath} markerEnd={markerEnd} style={style}>
        {payload && <title>{tooltip}</title>}
      </path>
    </>
  );

  if (!sourceUrl) {
    return paths;
  }

  return (
    <a href={sourceUrl} target="_blank" rel="noopener noreferrer" style={{ cursor }}>
      {paths}
    </a>
  );
}

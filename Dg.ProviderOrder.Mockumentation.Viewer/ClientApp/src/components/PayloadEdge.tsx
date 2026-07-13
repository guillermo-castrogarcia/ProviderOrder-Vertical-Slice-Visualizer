import { getBezierPath, type Edge, type EdgeProps } from '@xyflow/react';

export interface PayloadEdgeData {
  /** Simple name of the payload travelling along the arrow. */
  payload: string;
  [key: string]: unknown;
}

export type PayloadEdgeType = Edge<PayloadEdgeData, 'payload'>;

/**
 * Default-looking edge that reveals the payload name in a native tooltip on hover. A wide, transparent
 * companion path enlarges the hover hit-area so the thin arrow is easy to point at.
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

  return (
    <>
      <path d={edgePath} fill="none" stroke="transparent" strokeWidth={16} style={{ cursor: 'help' }}>
        {payload && <title>{payload}</title>}
      </path>
      <path id={id} className="react-flow__edge-path" d={edgePath} markerEnd={markerEnd} style={style}>
        {payload && <title>{payload}</title>}
      </path>
    </>
  );
}

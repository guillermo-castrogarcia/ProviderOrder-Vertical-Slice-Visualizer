import { type Node, type NodeProps } from '@xyflow/react';

/** Data for the outer bounding box around a whole application-side region (all Commands / all Queries). */
export interface SideBoxData {
  label: string; // "Commands" / "Queries"
  border: string; // side accent colour
  [key: string]: unknown;
}

export type SideBoxNode = Node<SideBoxData, 'sideBox'>;

/**
 * The outer region box. Wraps every product box of one side, carries the side colour (replacing the old
 * per-node side border) and a big label. Non-interactive (pointer-events disabled in CSS).
 */
export function SideBox({ data }: NodeProps<SideBoxNode>) {
  return (
    <div className="side-box" style={{ borderColor: data.border }}>
      <span className="side-box-label" style={{ color: data.border }}>
        {data.label}
      </span>
    </div>
  );
}

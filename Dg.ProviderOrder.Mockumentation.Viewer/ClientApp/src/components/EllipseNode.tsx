import { Handle, Position, type NodeProps, type Node } from '@xyflow/react';
import type { SliceNodeData } from '../types';
import { adapterColor, adapterLabel, categoryColor, categoryLabel, sideAccent } from '../theme';
import { NODE_H, NODE_W } from '../layout';

type SliceNode = Node<SliceNodeData, 'slice'>;

const STUB_LEN = 26;
const STUB_ROW = 14;

export function EllipseNode({ data }: NodeProps<SliceNode>) {
  const external = data.externalIncoming;
  const stubHeight = Math.max(external.length * STUB_ROW, 0);

  return (
    <div className="slice-node" style={{ width: NODE_W, height: NODE_H }}>
      {/* Free-standing incoming arrows for input that comes from outside any known slice. */}
      {external.length > 0 && (
        <svg
          className="external-incoming"
          width={STUB_LEN}
          height={NODE_H}
          style={{ left: -STUB_LEN, top: 0 }}
        >
          {external.map((incoming, i) => {
            const y = (NODE_H - stubHeight) / 2 + i * STUB_ROW + STUB_ROW / 2;
            const color = adapterColor[incoming.adapterType];
            const tip = incoming.payload
              ? `${incoming.payload}  ·  external ${adapterLabel[incoming.adapterType]}`
              : `External ${adapterLabel[incoming.adapterType]} input`;
            return (
              <g key={`${incoming.adapterType}|${incoming.payload}`}>
                {/* Wide transparent hit-area so the thin arrow is easy to hover; carries the tooltip. */}
                <rect
                  x={0}
                  y={y - STUB_ROW / 2}
                  width={STUB_LEN}
                  height={STUB_ROW}
                  fill="transparent"
                  pointerEvents="all"
                  style={{ cursor: 'help' }}
                >
                  <title>{tip}</title>
                </rect>
                <g stroke={color} fill={color} pointerEvents="none">
                  <line x1={0} y1={y} x2={STUB_LEN - 6} y2={y} strokeWidth={2} />
                  <path d={`M ${STUB_LEN - 8},${y - 4} L ${STUB_LEN},${y} L ${STUB_LEN - 8},${y + 4} Z`} stroke="none" />
                </g>
              </g>
            );
          })}
        </svg>
      )}

      <svg className="ellipse-bg" width={NODE_W} height={NODE_H}>
        {/* Fill = adapter category (Web/NServiceBus/Kafka/Other/Mixed); border = application side. */}
        <ellipse
          cx={NODE_W / 2}
          cy={NODE_H / 2}
          rx={NODE_W / 2 - 2}
          ry={NODE_H / 2 - 2}
          fill={categoryColor[data.adapterCategory]}
          stroke={sideAccent[data.side]}
          strokeWidth={3}
        />
      </svg>

      <Handle type="target" position={Position.Left} className="node-handle" />
      <div
        className="node-label"
        title={`${data.side} · ${data.product}\n${categoryLabel[data.adapterCategory]}\n${data.label}`}
      >
        {data.label}
      </div>
      <Handle type="source" position={Position.Right} className="node-handle" />
    </div>
  );
}

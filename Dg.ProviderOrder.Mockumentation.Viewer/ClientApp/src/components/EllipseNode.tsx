import { useEffect, type ReactNode } from 'react';
import { Handle, Position, useUpdateNodeInternals, type NodeProps, type Node } from '@xyflow/react';
import type { Adapter, AdapterType, DbAccess, Link, SliceNodeData } from '../types';
import { adapterColor, adapterLabel, categoryColor, categoryLabel, nodeOutline } from '../theme';
import { EXP_H, EXP_W, NODE_H, NODE_W } from '../layout';

type SliceNode = Node<SliceNodeData, 'slice'>;

const STUB_LEN = 26;
const STUB_ROW = 14;

// When expanded, an inter-slice arrow connects to a specific sub-box handle instead of the node centre:
// the incoming adapter matching the payload's adapter type, and the outgoing box that publishes it. Both
// ends resolve to these ids (shared with App.tsx, which sets the matching edge sourceHandle/targetHandle).
export const inHandleId = (type: AdapterType) => `in-${type}`;
export const outHandleId = (type: 'NServiceBus' | 'Kafka' | 'Web') => `out-${type}`;

export function EllipseNode(props: NodeProps<SliceNode>) {
  const { id, data } = props;
  // The set of handles changes when the node expands/collapses; React Flow must re-measure them so edges
  // can find the new sub-box handles.
  const updateNodeInternals = useUpdateNodeInternals();
  useEffect(() => updateNodeInternals(id), [id, data.expanded, updateNodeInternals]);
  return data.expanded ? <ExpandedNode id={id} data={data} /> : <CollapsedNode data={data} />;
}

// ---- Collapsed: the original ellipse ---------------------------------------------------------

function CollapsedNode({ data }: { data: SliceNodeData }) {
  const external = data.externalIncoming;
  const stubHeight = Math.max(external.length * STUB_ROW, 0);

  return (
    <div className="slice-node slice-node--collapsed" style={{ width: NODE_W, height: NODE_H }}>
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
        {/* Fill = adapter category (Web/NServiceBus/Kafka/Other/Mixed). Application side is now shown by
            the enclosing region box, so the ellipse only carries a neutral outline for definition. */}
        <ellipse
          cx={NODE_W / 2}
          cy={NODE_H / 2}
          rx={NODE_W / 2 - 2}
          ry={NODE_H / 2 - 2}
          fill={categoryColor[data.adapterCategory]}
          stroke={nodeOutline}
          strokeWidth={1.5}
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

// ---- Expanded: the slice internals, laid out like the mockup ---------------------------------

/** Heading shown above an incoming adapter, by its type. */
const adapterHeading: Record<AdapterType, string> = {
  Kafka: 'Kafka Consumer',
  NServiceBus: 'NSB Receiver',
  Web: 'Controller Action',
  Other: 'Adapter',
};

function ExpandedNode({ id, data }: { id: string; data: SliceNodeData }) {
  const d = data.detail;

  // Put the incoming handle on the FIRST adapter card of each type (handle ids must be unique per node).
  const seenIn = new Set<string>();
  const adapterHandleIds = d.adapters.map((a) => {
    const hid = inHandleId(a.adapterType);
    if (seenIn.has(hid)) return undefined;
    seenIn.add(hid);
    return hid;
  });

  // Inputs that arrive from outside any known slice have no inter-slice edge, so draw an incoming stub on
  // the matching adapter card (grouped by adapter type; shown on that type's first card, next to its handle).
  const externalByType = new Map<AdapterType, string[]>();
  for (const e of data.externalIncoming) {
    const arr = externalByType.get(e.adapterType) ?? externalByType.set(e.adapterType, []).get(e.adapterType)!;
    arr.push(e.payload);
  }

  return (
    <div className="slice-node slice-node--expanded" style={{ width: EXP_W, height: EXP_H }}>
      {/* Fallback centre handles for any edge that doesn't resolve to a specific sub-box handle. */}
      <Handle type="target" position={Position.Left} className="node-handle" />

      <div className="exp-header">
        <span className="exp-title">Vertical Slice</span>
        <span className="exp-slice-label" title={data.label}>{data.label}</span>
        <button
          type="button"
          className="exp-collapse nodrag nopan"
          title="Collapse"
          onClick={(e) => {
            e.stopPropagation();
            data.onCollapse(id);
          }}
        >
          ✕
        </button>
      </div>

      <div className="exp-body">
        {/* Left: incoming adapters */}
        <div className="exp-col exp-col--in">
          {d.adapters.map((a, i) => (
            <AdapterCard
              key={`${a.className}|${a.entryMethod}|${i}`}
              adapter={a}
              handleId={adapterHandleIds[i]}
              externalPayloads={adapterHandleIds[i] ? externalByType.get(a.adapterType) : undefined}
            />
          ))}
        </div>

        {/* Center: mediator payload, handling service, and read/written data */}
        <div className="exp-col exp-col--mid">
          {d.mediatorPayload && (
            <div className="exp-mediator">
              <span className="exp-mediator-label">Mediator / Marinator</span>
              <LinkOrText link={d.mediatorPayload} className="exp-mediator-name" />
            </div>
          )}

          {d.handler && (
            <div className="exp-handler">
              <div className="exp-box-title">Handling Service</div>
              <SourceLink url={d.handler.classUrl} className="exp-primary">{d.handler.className}</SourceLink>
              {d.handler.methodName && (
                <SourceLink url={d.handler.methodUrl} className="exp-secondary">
                  {`${d.handler.methodName}()`}
                </SourceLink>
              )}
            </div>
          )}

          {(d.reads.length > 0 || d.writes.length > 0) && (
            <div className="exp-data-row">
              <DataBox title="Read data" rows={d.reads} kind="read" />
              <DataBox title="Written data" rows={d.writes} kind="write" />
            </div>
          )}
        </div>

        {/* Right: outgoing messages / requests */}
        <div className="exp-col exp-col--out">
          <OutGroup title="Nsb Event/Command" links={d.nsbOut} accent={adapterColor.NServiceBus} handleId={outHandleId('NServiceBus')} />
          <OutGroup title="Kafka Event" links={d.kafkaOut} accent={adapterColor.Kafka} handleId={outHandleId('Kafka')} />
          <OutGroup
            title="Web Request"
            links={d.webRequests.map((name) => ({ name, url: null }))}
            accent={adapterColor.Web}
            handleId={outHandleId('Web')}
          />
        </div>
      </div>

      <Handle type="source" position={Position.Right} className="node-handle" />
    </div>
  );
}

function AdapterCard({
  adapter,
  handleId,
  externalPayloads,
}: {
  adapter: Adapter;
  handleId?: string;
  externalPayloads?: string[];
}) {
  const color = adapterColor[adapter.adapterType];
  const external = externalPayloads && externalPayloads.length > 0 ? externalPayloads : null;
  return (
    <div className="exp-adapter" style={{ borderLeftColor: color }}>
      {handleId && <Handle type="target" position={Position.Left} id={handleId} className="node-handle" />}
      {external && (
        <svg className="exp-adapter-stub" width={26} height={16}>
          <rect x={0} y={0} width={26} height={16} fill="transparent" pointerEvents="all" style={{ cursor: 'help' }}>
            <title>{`${external.join(', ')}  ·  external ${adapterLabel[adapter.adapterType]}`}</title>
          </rect>
          <g stroke={color} fill={color} pointerEvents="none">
            <line x1={0} y1={8} x2={18} y2={8} strokeWidth={2} />
            <path d="M 16,4 L 26,8 L 16,12 Z" stroke="none" />
          </g>
        </svg>
      )}
      <div className="exp-box-title" style={{ color }}>{adapterHeading[adapter.adapterType]}</div>
      <SourceLink url={adapter.classUrl} className="exp-primary">{adapter.className}</SourceLink>
      {adapter.adapterType === 'Web' && adapter.entryMethod && (
        <SourceLink url={adapter.entryMethodUrl} className="exp-secondary">{adapter.entryMethod}()</SourceLink>
      )}
    </div>
  );
}

/** Groups flat db accesses by database → schema → table, listing columns under each table.
 *  Renders nothing when there are no accesses, so empty read/written boxes never appear. */
function DataBox({ title, rows, kind }: { title: string; rows: DbAccess[]; kind: 'read' | 'write' }) {
  if (rows.length === 0) return null;
  const tree = groupAccesses(rows);
  return (
    <div className={`exp-data exp-data--${kind}`}>
      <div className="exp-box-title">{title}</div>
      <div className="exp-data-scroll">
        {tree.map((db) => (
          <div className="exp-db" key={db.name}>
            <div className="exp-db-name">{db.name}</div>
            {db.schemas.map((sc) => (
              <div className="exp-schema" key={sc.name}>
                <div className="exp-schema-name">{sc.name}</div>
                {sc.tables.map((tb) => (
                  <div className="exp-table" key={tb.name}>
                    <div className="exp-table-name">{tb.name}</div>
                    {tb.columns.map((col) => (
                      <div className="exp-column" key={col}>{col}</div>
                    ))}
                  </div>
                ))}
              </div>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

/** Renders nothing when there are no outgoing links, so empty NSB/Kafka/Web boxes never appear. */
function OutGroup({ title, links, accent, handleId }: { title: string; links: Link[]; accent: string; handleId: string }) {
  if (links.length === 0) return null;
  return (
    <div className="exp-out" style={{ borderLeftColor: accent }}>
      <Handle type="source" position={Position.Right} id={handleId} className="node-handle" />
      <div className="exp-box-title" style={{ color: accent }}>{title}</div>
      {links.map((l, i) => (
        <LinkOrText key={`${l.name}|${i}`} link={l} className="exp-primary" />
      ))}
    </div>
  );
}

function LinkOrText({ link, className }: { link: Link; className?: string }) {
  return (
    <SourceLink url={link.url} className={className}>
      {link.name}
    </SourceLink>
  );
}

/** Renders text as a GitHub link when a source URL exists, otherwise as plain text. */
function SourceLink({ url, className, children }: { url?: string | null; className?: string; children: ReactNode }) {
  if (!url) {
    return <span className={className}>{children}</span>;
  }
  return (
    <a
      className={`${className ?? ''} exp-link nodrag nopan`}
      href={url}
      target="_blank"
      rel="noopener noreferrer"
      title="Open source on GitHub"
      onClick={(e) => e.stopPropagation()}
      onMouseDown={(e) => e.stopPropagation()}
    >
      {children}
    </a>
  );
}

interface DbGroup {
  name: string;
  schemas: { name: string; tables: { name: string; columns: string[] }[] }[];
}

function groupAccesses(rows: DbAccess[]): DbGroup[] {
  const dbs = new Map<string, Map<string, Map<string, string[]>>>();
  for (const r of rows) {
    const schemas = dbs.get(r.database) ?? dbs.set(r.database, new Map()).get(r.database)!;
    const tables = schemas.get(r.schema) ?? schemas.set(r.schema, new Map()).get(r.schema)!;
    const cols = tables.get(r.table) ?? tables.set(r.table, []).get(r.table)!;
    if (!cols.includes(r.column)) cols.push(r.column);
  }
  return [...dbs.entries()].map(([name, schemas]) => ({
    name,
    schemas: [...schemas.entries()].map(([sname, tables]) => ({
      name: sname,
      tables: [...tables.entries()].map(([tname, columns]) => ({ name: tname, columns: columns.sort() })),
    })),
  }));
}

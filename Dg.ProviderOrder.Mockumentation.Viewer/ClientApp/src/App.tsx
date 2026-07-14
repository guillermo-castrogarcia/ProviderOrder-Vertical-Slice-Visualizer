import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Background,
  Controls,
  MarkerType,
  MiniMap,
  Panel,
  ReactFlow,
  ReactFlowProvider,
  useEdgesState,
  useNodesState,
  useReactFlow,
  type Edge,
  type Node,
  type NodeChange,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import './App.css';

import { fetchGraph } from './api';
import { computeLayout, nodeSize } from './layout';
import { adapterColor, categoryColor, categoryLabel, sideAccent } from './theme';
import { productBorder, productFill, productIndex } from './products';
import { EllipseNode, inHandleId, outHandleId } from './components/EllipseNode';
import { ProductBox } from './components/ProductBox';
import { SideBox } from './components/SideBox';
import { PayloadEdge } from './components/PayloadEdge';
import { CheckboxTree, type SideGroup } from './components/CheckboxTree';
import type { AdapterCategory, AdapterType, Graph, Side, SliceNodeData } from './types';

type SliceNode = Node<SliceNodeData, 'slice'>;

const nodeTypes = { slice: EllipseNode, productBox: ProductBox, sideBox: SideBox };
const edgeTypes = { payload: PayloadEdge };
const SIDES: Side[] = ['Command', 'Query'];
const SIDE_LABEL: Record<Side, string> = { Command: 'Commands', Query: 'Queries' };

// Inner product box padding, and the larger outer side-box padding (must exceed the product padding so
// the side box visibly encloses its product boxes; the extra top room seats the "Commands"/"Queries" label).
const BOX_PAD = 28;
const BOX_PAD_TOP = 46;
const SIDE_BOX_PAD = 54;
const SIDE_BOX_PAD_TOP = 92;
const isBoxId = (id?: string) => !!id && (id.startsWith('productbox::') || id.startsWith('sidebox::'));
const noop = () => {};

interface Bounds {
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
}

function extend(map: Map<string, Bounds>, key: string, x: number, y: number, w: number, h: number): void {
  const b = map.get(key);
  if (!b) {
    map.set(key, { minX: x, minY: y, maxX: x + w, maxY: y + h });
  } else {
    b.minX = Math.min(b.minX, x);
    b.minY = Math.min(b.minY, y);
    b.maxX = Math.max(b.maxX, x + w);
    b.maxY = Math.max(b.maxY, y + h);
  }
}

/**
 * Derives the bounding-box nodes from the CURRENT positions of the visible slice nodes, so the boxes track
 * live layout — dragging a slice or toggling re-fits them every frame. Two levels: an outer side box per
 * application side (all Commands / all Queries) and an inner box per (side, product). Side boxes are
 * emitted first and given the lowest z so they sit behind the product boxes, which sit behind the slices.
 */
function computeBoxNodes(sliceNodes: SliceNode[], expanded: ReadonlySet<string>): Node[] {
  const sideBounds = new Map<string, Bounds>(); // key: side
  const productBounds = new Map<string, Bounds>(); // key: `${side}::${product}`
  for (const n of sliceNodes) {
    if (n.hidden) continue;
    const { x, y } = n.position;
    const { w, h } = nodeSize(n.id, expanded);
    extend(sideBounds, n.data.side, x, y, w, h);
    extend(productBounds, `${n.data.side}::${n.data.product}`, x, y, w, h);
  }

  const sideBoxes: Node[] = SIDES.filter((s) => sideBounds.has(s)).map((side) => {
    const b = sideBounds.get(side)!;
    const width = b.maxX - b.minX + SIDE_BOX_PAD * 2;
    const height = b.maxY - b.minY + SIDE_BOX_PAD_TOP + SIDE_BOX_PAD;
    return {
      id: `sidebox::${side}`,
      type: 'sideBox',
      position: { x: b.minX - SIDE_BOX_PAD, y: b.minY - SIDE_BOX_PAD_TOP },
      width,
      height,
      data: { label: SIDE_LABEL[side], border: sideAccent[side] },
      draggable: false,
      selectable: false,
      connectable: false,
      deletable: false,
      focusable: false,
      zIndex: 0, // outermost / behind everything
      style: { width, height, pointerEvents: 'none' },
    } satisfies Node;
  });

  const productBoxes: Node[] = [...productBounds.entries()]
    .sort((a, b) => productIndex(a[0].split('::')[1]) - productIndex(b[0].split('::')[1]))
    .map(([key, b]) => {
      const product = key.split('::')[1];
      const width = b.maxX - b.minX + BOX_PAD * 2;
      const height = b.maxY - b.minY + BOX_PAD_TOP + BOX_PAD;
      return {
        id: `productbox::${key}`,
        type: 'productBox',
        position: { x: b.minX - BOX_PAD, y: b.minY - BOX_PAD_TOP },
        width,
        height,
        data: { product, fill: productFill(product), border: productBorder(product) },
        draggable: false,
        selectable: false,
        connectable: false,
        deletable: false,
        focusable: false,
        zIndex: 1, // between the side boxes (0) and the slices (2)
        style: { width, height, pointerEvents: 'none' },
      } satisfies Node;
    });

  return [...sideBoxes, ...productBoxes];
}

const keyOf = (side: string, product: string) => `${side}::${product}`;

function buildElements(graph: Graph): { nodes: SliceNode[]; edges: Edge[] } {
  const positions = computeLayout(graph.nodes, graph.edges);
  const nodes: SliceNode[] = graph.nodes.map((n) => ({
    id: n.id,
    type: 'slice',
    position: positions.get(n.id) ?? { x: 0, y: 0 },
    zIndex: 2, // above both the side boxes (0) and product boxes (1)
    data: {
      label: n.label,
      side: n.side,
      product: n.product,
      externalIncoming: n.externalIncoming,
      adapterCategory: n.adapterCategory,
      detail: n.detail,
      expanded: false,
      onCollapse: noop,
    },
  }));

  const edges: Edge[] = graph.edges.map((e) => {
    const color = adapterColor[e.adapterType];
    return {
      id: e.id,
      source: e.source,
      target: e.target,
      type: 'payload',
      data: { payload: e.payload, sourceUrl: e.sourceUrl ?? null },
      style: { stroke: color, strokeWidth: 2 },
      markerEnd: { type: MarkerType.ArrowClosed, color, width: 18, height: 18 },
    };
  });

  return { nodes, edges };
}

function buildTree(graph: Graph): SideGroup[] {
  return SIDES.map((side) => {
    const counts = new Map<string, number>();
    for (const n of graph.nodes) {
      if (n.side === side) counts.set(n.product, (counts.get(n.product) ?? 0) + 1);
    }
    const products = [...counts.entries()]
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([product, count]) => ({ product, key: keyOf(side, product), count }));
    return { side, products };
  }).filter((g) => g.products.length > 0);
}

function Flow({ graph }: { graph: Graph }) {
  const [nodes, setNodes, onNodesChange] = useNodesState<SliceNode>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [hidden, setHidden] = useState<Set<string>>(new Set());
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const { fitView } = useReactFlow();

  const tree = useMemo(() => buildTree(graph), [graph]);

  const collapse = useCallback((id: string) => {
    setExpanded((prev) => {
      if (!prev.has(id)) return prev;
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
  }, []);

  const toggleExpanded = useCallback((id: string) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);

  // Product bounding boxes are derived from the live slice positions and rendered behind them, so they
  // re-fit on every drag and every animation frame. onNodesChange only carries slice-node changes.
  const boxNodes = useMemo(() => computeBoxNodes(nodes, expanded), [nodes, expanded]);
  // Inject the live expanded flag + collapse callback + intended size/z onto each slice node. The base
  // node state keeps only positions (mutated by the animation); expansion is layered on here per render.
  const sliceRfNodes = useMemo<Node[]>(
    () =>
      nodes.map((n) => {
        const isExpanded = expanded.has(n.id);
        const { w, h } = nodeSize(n.id, expanded);
        return {
          ...n,
          width: w,
          height: h,
          zIndex: isExpanded ? 5 : 2,
          data: { ...n.data, expanded: isExpanded, onCollapse: collapse },
        };
      }),
    [nodes, expanded, collapse],
  );
  const rfNodes = useMemo<Node[]>(() => [...boxNodes, ...sliceRfNodes], [boxNodes, sliceRfNodes]);
  const handleNodesChange = useCallback(
    (changes: NodeChange[]) => onNodesChange(changes.filter((c) => !('id' in c) || !isBoxId(c.id)) as NodeChange<SliceNode>[]),
    [onNodesChange],
  );

  // Products present in the graph, in declaration order, for the colour legend.
  const legendProducts = useMemo(() => {
    const set = new Set(graph.nodes.map((n) => n.product));
    return [...set].sort((a, b) => productIndex(a) - productIndex(b) || a.localeCompare(b));
  }, [graph]);

  // Latest node list + running animation frame + previously-hidden set, read synchronously by animateLayout.
  const nodesRef = useRef<SliceNode[]>([]);
  nodesRef.current = nodes;
  const animRef = useRef<number | null>(null);
  const prevHiddenRef = useRef<Set<string>>(new Set());

  // Re-pack the visible slices (filling any gaps the hidden ones leave) and tween every node from its
  // current spot to the new one over ~450ms. Positions live in node state, so edges follow each frame.
  // Slices that just became visible fade in at their target while the others glide aside.
  const animateLayout = useCallback(
    (hiddenSet: Set<string>) => {
      const visibleGraphNodes = graph.nodes.filter((n) => !hiddenSet.has(keyOf(n.side, n.product)));
      const target = computeLayout(visibleGraphNodes, graph.edges, expanded);
      const prevHidden = prevHiddenRef.current;
      prevHiddenRef.current = new Set(hiddenSet);

      const appearing = new Set(
        graph.nodes
          .filter((n) => {
            const key = keyOf(n.side, n.product);
            return prevHidden.has(key) && !hiddenSet.has(key);
          })
          .map((n) => n.id),
      );
      const start = new Map(nodesRef.current.map((n) => [n.id, n.position]));

      // Seed the frame: flip hidden flags; appearing slices jump to target and start transparent.
      setNodes((ns) =>
        ns.map((n) => {
          const isHidden = hiddenSet.has(keyOf(n.data.side, n.data.product));
          if (appearing.has(n.id)) {
            return { ...n, hidden: false, position: target.get(n.id) ?? n.position, style: { ...n.style, opacity: 0 } };
          }
          return { ...n, hidden: isHidden };
        }),
      );

      if (animRef.current !== null) cancelAnimationFrame(animRef.current);
      const DURATION = 450;
      const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);
      let startTime: number | null = null;

      const step = (now: number) => {
        if (startTime === null) startTime = now;
        const k = easeOutCubic(Math.min(1, (now - startTime) / DURATION));
        setNodes((ns) =>
          ns.map((n) => {
            const to = target.get(n.id);
            if (!to) return n; // hidden slice — leave it be
            if (appearing.has(n.id)) {
              return { ...n, position: to, style: { ...n.style, opacity: k } };
            }
            const from = start.get(n.id) ?? to;
            return { ...n, position: { x: from.x + (to.x - from.x) * k, y: from.y + (to.y - from.y) * k } };
          }),
        );
        if (k < 1) {
          animRef.current = requestAnimationFrame(step);
        } else {
          animRef.current = null;
          setNodes((ns) => ns.map((n) => (appearing.has(n.id) ? { ...n, style: { ...n.style, opacity: 1 } } : n)));
        }
      };
      animRef.current = requestAnimationFrame(step);
    },
    [graph, setNodes, expanded],
  );

  // Initial build.
  useEffect(() => {
    const built = buildElements(graph);
    setNodes(built.nodes);
    setEdges(built.edges);
    setHidden(new Set());
    setExpanded(new Set());
    prevHiddenRef.current = new Set();
  }, [graph, setNodes, setEdges]);

  // Animate a re-pack whenever the hidden set OR the expanded set changes (the latter recreates
  // animateLayout, since it now closes over `expanded`, so this effect re-runs and neighbours reflow).
  useEffect(() => {
    animateLayout(hidden);
  }, [hidden, animateLayout]);

  // Stop any in-flight animation on unmount.
  useEffect(() => () => {
    if (animRef.current !== null) cancelAnimationFrame(animRef.current);
  }, []);

  const hiddenNodeIds = useMemo(() => {
    const ids = new Set<string>();
    for (const n of graph.nodes) {
      if (hidden.has(keyOf(n.side, n.product))) ids.add(n.id);
    }
    return ids;
  }, [graph, hidden]);

  useEffect(() => {
    setEdges((es) =>
      es.map((e) => ({ ...e, hidden: hiddenNodeIds.has(e.source) || hiddenNodeIds.has(e.target) })),
    );
  }, [hiddenNodeIds, setEdges]);

  // Which sub-box handles each node exposes when expanded (must mirror EllipseNode's rendered handles).
  const nodeHandles = useMemo(() => {
    const m = new Map<string, { inc: Set<string>; out: Set<string> }>();
    for (const n of graph.nodes) {
      const inc = new Set<string>();
      for (const a of n.detail.adapters) inc.add(inHandleId(a.adapterType));
      const out = new Set<string>();
      if (n.detail.nsbOut.length) out.add(outHandleId('NServiceBus'));
      if (n.detail.kafkaOut.length) out.add(outHandleId('Kafka'));
      if (n.detail.webRequests.length) out.add(outHandleId('Web'));
      m.set(n.id, { inc, out });
    }
    return m;
  }, [graph]);

  // adapterType lives on the graph edge, not the React Flow edge — index it for handle routing.
  const edgeMeta = useMemo(() => {
    const m = new Map<string, { source: string; target: string; adapterType: AdapterType }>();
    for (const e of graph.edges) m.set(e.id, { source: e.source, target: e.target, adapterType: e.adapterType });
    return m;
  }, [graph]);

  // When an endpoint is expanded, route the edge to the matching sub-box handle (the incoming adapter of
  // the payload's type on the consumer, the outgoing box that publishes it on the producer); otherwise use
  // the node-centre fallback handle. Only assigns a handle that actually exists on that node.
  useEffect(() => {
    setEdges((es) =>
      es.map((e) => {
        const meta = edgeMeta.get(e.id);
        if (!meta) return e;
        const wantIn = expanded.has(meta.target) ? inHandleId(meta.adapterType) : null;
        const wantOut = expanded.has(meta.source)
          ? outHandleId(meta.adapterType as 'NServiceBus' | 'Kafka' | 'Web')
          : null;
        const targetHandle = wantIn && nodeHandles.get(meta.target)?.inc.has(wantIn) ? wantIn : null;
        const sourceHandle = wantOut && nodeHandles.get(meta.source)?.out.has(wantOut) ? wantOut : null;
        if (e.sourceHandle === sourceHandle && e.targetHandle === targetHandle) return e;
        return { ...e, sourceHandle, targetHandle };
      }),
    );
  }, [expanded, edgeMeta, nodeHandles, setEdges]);

  const toggleProduct = useCallback((key: string, visible: boolean) => {
    setHidden((prev) => {
      const next = new Set(prev);
      if (visible) next.delete(key);
      else next.add(key);
      return next;
    });
  }, []);

  const toggleSide = useCallback(
    (side: Side, visible: boolean) => {
      const keys = tree.find((g) => g.side === side)?.products.map((p) => p.key) ?? [];
      setHidden((prev) => {
        const next = new Set(prev);
        for (const k of keys) {
          if (visible) next.delete(k);
          else next.add(k);
        }
        return next;
      });
    },
    [tree],
  );

  const resetLayout = useCallback(() => {
    animateLayout(hidden);
    window.setTimeout(() => fitView({ padding: 0.1, duration: 400 }), 0);
  }, [animateLayout, hidden, fitView]);

  const visibleCount = nodes.length - hiddenNodeIds.size;

  return (
    <div className="app">
      <header className="toolbar">
        <h1>Vertical Slice Viewer</h1>
        <button className="reset-btn" onClick={resetLayout}>
          Reset layout
        </button>
        <div className="legend">
          {(Object.keys(categoryColor) as AdapterCategory[]).map((c) => (
            <span className="legend-item" key={c}>
              <span className="legend-dot" style={{ background: categoryColor[c] }} />
              {categoryLabel[c]}
            </span>
          ))}
          <span className="legend-sep" />
          {(Object.keys(sideAccent) as Side[]).map((s) => (
            <span className="legend-item" key={s}>
              <span className="legend-dot legend-dot--ring" style={{ borderColor: sideAccent[s] }} />
              {s}
            </span>
          ))}
        </div>
        <span className="count">{visibleCount} slices</span>
      </header>

      <div className="body">
        <aside className="sidebar">
          <h2>Products</h2>
          <CheckboxTree tree={tree} hidden={hidden} onToggleProduct={toggleProduct} onToggleSide={toggleSide} />
        </aside>

        <main className="canvas">
          <ReactFlow
            nodes={rfNodes}
            edges={edges}
            nodeTypes={nodeTypes}
            edgeTypes={edgeTypes}
            onNodesChange={handleNodesChange}
            onEdgesChange={onEdgesChange}
            onNodeDoubleClick={(_, node) => {
              if (node.type === 'slice') toggleExpanded(node.id);
            }}
            fitView
            minZoom={0.05}
            maxZoom={2}
            nodesConnectable={false}
            proOptions={{ hideAttribution: true }}
          >
            <Background gap={24} />
            <MiniMap pannable zoomable nodeStrokeWidth={3} />
            <Controls />
            <Panel position="top-left" className="product-legend">
              <div className="product-legend-title">Products</div>
              {legendProducts.map((p) => (
                <div className="product-legend-item" key={p}>
                  <span
                    className="product-legend-swatch"
                    style={{ background: productFill(p), borderColor: productBorder(p) }}
                  />
                  {p}
                </div>
              ))}
            </Panel>
          </ReactFlow>
        </main>
      </div>
    </div>
  );
}

export default function App() {
  const [graph, setGraph] = useState<Graph | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchGraph().then(setGraph).catch((e) => setError(String(e)));
  }, []);

  if (error) return <div className="status error">{error}</div>;
  if (!graph) return <div className="status">Loading graph…</div>;

  return (
    <ReactFlowProvider>
      <Flow graph={graph} />
    </ReactFlowProvider>
  );
}

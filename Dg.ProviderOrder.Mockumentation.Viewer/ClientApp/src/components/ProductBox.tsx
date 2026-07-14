import { type Node, type NodeProps } from '@xyflow/react';

/** Data for a product swimlane bounding box. Sized/positioned by App from its members' live positions. */
export interface ProductBoxData {
  product: string;
  fill: string;
  border: string;
  [key: string]: unknown;
}

export type ProductBoxNode = Node<ProductBoxData, 'productBox'>;

/**
 * A translucent, dashed bounding box drawn behind the slices of one product. Non-interactive
 * (pointer-events are disabled in CSS) so it never intercepts panning or node dragging.
 */
export function ProductBox({ data }: NodeProps<ProductBoxNode>) {
  return (
    <div className="product-box" style={{ background: data.fill, borderColor: data.border }}>
      <span className="product-box-label" style={{ color: data.border }}>
        {data.product}
      </span>
    </div>
  );
}

import { useEffect, useRef } from 'react';
import type { Side } from '../types';
import { sideAccent } from '../theme';

export interface ProductLeaf {
  product: string;
  key: string; // `${side}::${product}`
  count: number;
}

export interface SideGroup {
  side: Side;
  products: ProductLeaf[];
}

interface Props {
  tree: SideGroup[];
  hidden: Set<string>;
  onToggleProduct: (key: string, visible: boolean) => void;
  onToggleSide: (side: Side, visible: boolean) => void;
}

function TriStateCheckbox({
  checked,
  indeterminate,
  onChange,
}: {
  checked: boolean;
  indeterminate: boolean;
  onChange: (checked: boolean) => void;
}) {
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate && !checked;
  }, [indeterminate, checked]);
  return <input ref={ref} type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} />;
}

export function CheckboxTree({ tree, hidden, onToggleProduct, onToggleSide }: Props) {
  return (
    <div className="tree">
      {tree.map((group) => {
        const total = group.products.length;
        const hiddenCount = group.products.filter((p) => hidden.has(p.key)).length;
        const allVisible = hiddenCount === 0;
        const someVisible = hiddenCount < total;
        const sliceCount = group.products.reduce((sum, p) => sum + p.count, 0);
        return (
          <div className="tree-group" key={group.side}>
            <label className="tree-side" style={{ borderLeftColor: sideAccent[group.side] }}>
              <TriStateCheckbox
                checked={allVisible}
                indeterminate={someVisible}
                onChange={(visible) => onToggleSide(group.side, visible)}
              />
              <span className="tree-side-name">{group.side}</span>
              <span className="tree-count">{sliceCount}</span>
            </label>
            <div className="tree-children">
              {group.products.map((p) => (
                <label className="tree-leaf" key={p.key}>
                  <input
                    type="checkbox"
                    checked={!hidden.has(p.key)}
                    onChange={(e) => onToggleProduct(p.key, e.target.checked)}
                  />
                  <span className="tree-leaf-name">{p.product}</span>
                  <span className="tree-count">{p.count}</span>
                </label>
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
}

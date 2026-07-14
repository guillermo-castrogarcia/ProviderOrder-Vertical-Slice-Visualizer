// Product identity for the vertical swimlane layout.
//
// The vertical order of the product bands mirrors the declaration order of
// Dg.ProviderOrder.Mokumentation.ProviderOrderProduct (see ProviderOrderExtensions.cs). Keep these in
// sync: a slice's `product` string is one of these names (produced by ProviderOrderDomain.FindProduct).

/** Canonical product order — matches the ProviderOrderProduct enum declaration order. */
export const PRODUCT_ORDER = [
  'OrderCreation',
  'OrderExport',
  'ProviderResponse',
  'ProviderDelivery',
  'ProviderInvoice',
  'Returns',
  'Cancellation',
  'ProviderRuling',
  'Unknown',
] as const;

// One hue per product, drawn from the data-viz reference categorical set (dark-surface steps, since the
// canvas is dark). Bands are rendered as a translucent wash + dashed border, and each band carries its
// product name as a label, so identity never rests on colour alone. Unknown folds into a neutral gray
// ("Other") rather than taking a 9th generated hue.
export const productHue: Record<string, string> = {
  OrderCreation: '#3987e5', // blue
  OrderExport: '#199e70', // aqua
  ProviderResponse: '#c98500', // yellow
  ProviderDelivery: '#008300', // green
  ProviderInvoice: '#9085e9', // violet
  Returns: '#e66767', // red
  Cancellation: '#d55181', // magenta
  ProviderRuling: '#d95926', // orange
  Unknown: '#64748b', // slate — neutral "Other"
};

const FALLBACK_HUE = '#64748b';

/** Vertical band order for a product; unknown products sort after all declared ones. */
export function productIndex(product: string): number {
  const i = (PRODUCT_ORDER as readonly string[]).indexOf(product);
  return i === -1 ? PRODUCT_ORDER.length : i;
}

export function productHueOf(product: string): string {
  return productHue[product] ?? FALLBACK_HUE;
}

function hexToRgba(hex: string, alpha: number): string {
  const h = hex.replace('#', '');
  const r = parseInt(h.slice(0, 2), 16);
  const g = parseInt(h.slice(2, 4), 16);
  const b = parseInt(h.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

/** Light translucent wash for a product band's background. */
export function productFill(product: string): string {
  return hexToRgba(productHueOf(product), 0.13);
}

/** Stronger, but still soft, tone for the dashed band border. */
export function productBorder(product: string): string {
  return hexToRgba(productHueOf(product), 0.6);
}

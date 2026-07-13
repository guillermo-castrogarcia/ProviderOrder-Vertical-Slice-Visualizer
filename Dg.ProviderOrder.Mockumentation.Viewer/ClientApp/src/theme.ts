import type { AdapterCategory, AdapterType, Side } from './types';

/** Colour per primary-adapter type. Shared by node fills and arrows, so an arrow carrying an
 *  e.g. NServiceBus payload is drawn in the same colour as an NServiceBus node receiving it. */
export const adapterColor: Record<AdapterType, string> = {
  Web: '#3b82f6', // blue
  NServiceBus: '#f59e0b', // amber
  Kafka: '#ef4444', // red
  Other: '#8b5cf6', // violet
};

/** One shared colour for every slice that exposes adapters of several different types. */
export const mixedColor = '#64748b'; // slate

/** Node fill colour for a slice, by its adapter category (adds "Mixed" on top of the adapter types). */
export const categoryColor: Record<AdapterCategory, string> = {
  ...adapterColor,
  Mixed: mixedColor,
};

export const adapterLabel: Record<AdapterType, string> = {
  Web: 'Web request',
  NServiceBus: 'NServiceBus',
  Kafka: 'Kafka',
  Other: 'Other',
};

export const categoryLabel: Record<AdapterCategory, string> = {
  ...adapterLabel,
  Mixed: 'Mixed adapters',
};

/** Node border accent by application side, to reinforce the Command/Query grouping. */
export const sideAccent: Record<Side, string> = {
  Command: '#0ea5e9',
  Query: '#22c55e',
};

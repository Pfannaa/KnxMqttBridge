import type { KnxAddress } from '../types';

export function parseAddressesXml(xmlText: string): KnxAddress[] {
  const doc = new DOMParser().parseFromString(xmlText, 'application/xml');
  const addresses: KnxAddress[] = [];

  for (const topRange of doc.documentElement.children) {
    if (topRange.localName !== 'GroupRange') continue;
    const category = topRange.getAttribute('Name') ?? '';

    for (const subRange of topRange.children) {
      if (subRange.localName !== 'GroupRange') continue;
      const subcategory = subRange.getAttribute('Name') ?? '';

      for (const ga of subRange.children) {
        if (ga.localName !== 'GroupAddress') continue;
        const address = ga.getAttribute('Address');
        if (!address) continue;

        addresses.push({
          address,
          name: ga.getAttribute('Name') ?? address,
          dpt: ga.getAttribute('DPTs') ?? null,
          category,
          subcategory,
        });
      }
    }
  }

  return addresses;
}

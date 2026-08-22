/**
 * One shared category pictogram mapping.
 *
 * Category glyphs are *content* — they stand for the user's own categories —
 * so they stay pictorial rather than becoming line icons (the same choice
 * Copilot Money, YNAB and GoPay make). What they must not be is inconsistent:
 * this logic was previously duplicated in four screens with different rules,
 * so "Groceries" showed 🛒 on the Categories screen but the generic fallback
 * on the Expenses list, and "Food" disagreed the other way.
 *
 * Order matters: the most specific match wins, so put narrow terms first.
 */

type Rule = { match: string[]; glyph: string };

const RULES: Rule[] = [
  { match: ['grocer', 'supermarket'], glyph: '🛒' },
  { match: ['dining', 'restaurant', 'food', 'meal', 'coffee', 'snack'], glyph: '🍽️' },
  { match: ['transport', 'travel', 'car', 'fuel', 'taxi'], glyph: '🚗' },
  { match: ['flight', 'airline'], glyph: '✈️' },
  { match: ['entertainment', 'fun', 'film', 'movie', 'game'], glyph: '🎬' },
  { match: ['shopping', 'clothes', 'cart'], glyph: '🛍️' },
  { match: ['health', 'medical', 'pharmacy', 'heart'], glyph: '❤️' },
  { match: ['utilit', 'electric', 'water', 'light', 'plug'], glyph: '💡' },
  { match: ['bill', 'invoice', 'subscription'], glyph: '📄' },
  { match: ['housing', 'rent', 'mortgage', 'house', 'accommodation', 'hotel'], glyph: '🏠' },
  { match: ['education', 'school', 'book', 'course'], glyph: '📚' },
  { match: ['employment', 'salary', 'wage', 'work', 'briefcase'], glyph: '💼' },
  { match: ['freelance', 'side', 'gig'], glyph: '💻' },
  { match: ['invest', 'asset', 'graph', 'chart', 'dividend'], glyph: '📈' },
  { match: ['insurance', 'shield'], glyph: '🛡️' },
  { match: ['personal', 'self'], glyph: '🧴' },
  { match: ['gift', 'donation', 'charity'], glyph: '🎁' },
  { match: ['saving', 'goal'], glyph: '🏦' },
];

const FALLBACK = '📁';

/**
 * @param name  Category name from the API.
 * @param icon  Optional icon hint stored on the category. If it already holds
 *              an emoji, it is trusted over any name-based guess.
 */
export function categoryIcon(name?: string | null, icon?: string | null): string {
  const explicit = (icon || '').trim();
  // An icon field holding an actual emoji is the user's/seed's own choice.
  if (explicit && explicit.length <= 3 && !/^[a-z0-9 _-]+$/i.test(explicit)) {
    return explicit;
  }

  const haystack = `${name || ''} ${icon || ''}`.toLowerCase();
  if (!haystack.trim()) return FALLBACK;

  for (const rule of RULES) {
    if (rule.match.some((term) => haystack.includes(term))) return rule.glyph;
  }
  return FALLBACK;
}

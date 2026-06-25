/**
 * k6 load test – QuickCart checkout endpoint
 *
 * Measures the hottest API path: add-to-cart + POST /api/v1/orders.
 * This path is the most expensive because it does:
 *   1. Cart lookup (JOIN with CartItems)
 *   2. Product batch lookup (price snapshot)
 *   3. Order INSERT + Cart UPDATE in one transaction
 *   4. Domain event dispatch
 *
 * Usage:
 *   k6 run tests/performance/checkout.js
 *   k6 run --env BASE_URL=https://your-api.azurewebsites.net tests/performance/checkout.js
 *
 * Read p99 interpretation guide in Day31.md.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Custom metrics for each stage of the journey
const addToCartTrend  = new Trend('add_to_cart_duration',  true);
const checkoutTrend   = new Trend('checkout_duration',     true);
const errorRate       = new Rate('errors');

export const options = {
  stages: [
    { duration: '15s', target: 5  },   // warm-up: ramp up to 5 VUs
    { duration: '30s', target: 20 },   // sustained load: 20 VUs
    { duration: '15s', target: 40 },   // peak: 40 VUs
    { duration: '10s', target: 0  },   // ramp-down
  ],
  thresholds: {
    // Overall latency targets
    http_req_duration:            ['p(95)<500', 'p(99)<1000'],
    // Checkout-specific (the expensive operation)
    checkout_duration:            ['p(95)<600', 'p(99)<1200'],
    // Error rate must stay below 1%
    errors:                       ['rate<0.01'],
    // HTTP failure rate
    http_req_failed:              ['rate<0.01'],
  },
};

// ── Setup: discover a product ID from the seeded catalog ────────────────────
export function setup() {
  const res = http.get(`${BASE_URL}/api/v1/products`);
  if (res.status !== 200) {
    throw new Error(`Setup failed — could not load products: HTTP ${res.status}`);
  }
  const products = JSON.parse(res.body);
  const available = products.find(p => p.isAvailable);
  if (!available) {
    throw new Error('No available products in the catalog');
  }
  return { productId: available.productId };
}

// ── Default function (runs once per VU iteration) ───────────────────────────
export default function ({ productId }) {
  const headers = { 'Content-Type': 'application/json' };

  group('add_to_cart', () => {
    const start = Date.now();
    const res = http.post(
      `${BASE_URL}/api/v1/cart/items`,
      JSON.stringify({ productId, quantity: 1 }),
      { headers }
    );
    addToCartTrend.add(Date.now() - start);

    const ok = check(res, {
      'add_to_cart status 200': r => r.status === 200,
    });
    errorRate.add(!ok);
  });

  group('checkout', () => {
    const start = Date.now();
    const res = http.post(`${BASE_URL}/api/v1/orders`, null, { headers });
    checkoutTrend.add(Date.now() - start);

    const ok = check(res, {
      'checkout status 201': r => r.status === 201,
    });
    errorRate.add(!ok);
  });

  sleep(0.5); // 500 ms think-time between iterations
}

// ── Summary printed after the run ───────────────────────────────────────────
export function handleSummary(data) {
  const checkout = data.metrics.checkout_duration;
  if (!checkout) return {};

  console.log('=== Checkout Performance Summary ===');
  console.log(`  avg   : ${checkout.values.avg.toFixed(1)} ms`);
  console.log(`  p(95) : ${checkout.values['p(95)'].toFixed(1)} ms`);
  console.log(`  p(99) : ${checkout.values['p(99)'].toFixed(1)} ms`);
  console.log(`  min   : ${checkout.values.min.toFixed(1)} ms`);
  console.log(`  max   : ${checkout.values.max.toFixed(1)} ms`);
  console.log('====================================');

  return {};
}

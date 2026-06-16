export interface ChartDataPoint {
  readingId: number;
  timestamp: string;
  value: number;
}

export interface ChartTrimOptions {
  windowMode: 'time' | 'points';
  timeWindowMinutes: number;
  maxDataPoints: number;
}

/** Allow readings slightly ahead of client clock so live points are not dropped. */
export const LIVE_CLOCK_SKEW_MS = 5000;

/** API/SQL timestamps are UTC; ISO strings without an offset must not be parsed as local time. */
export function parseApiTimestampMs(timestamp: string): number {
  const trimmed = timestamp.trim();
  if (!trimmed) return NaN;
  if (/[Zz]$/.test(trimmed) || /[+-]\d{2}:\d{2}$/.test(trimmed)) {
    return new Date(trimmed).getTime();
  }
  if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
    return new Date(`${trimmed}T00:00:00Z`).getTime();
  }
  return new Date(`${trimmed}Z`).getTime();
}

function dedupeKey(point: ChartDataPoint): number {
  if (point.readingId > 0) {
    return point.readingId;
  }
  const fallback = `${point.timestamp}-${point.value}`;
  let hash = 0;
  for (let i = 0; i < fallback.length; i += 1) {
    hash = (hash * 31 + fallback.charCodeAt(i)) | 0;
  }
  return -Math.abs(hash) || -1;
}

export function dedupeChartPoints(points: ChartDataPoint[]): ChartDataPoint[] {
  const map = new Map<number, ChartDataPoint>();
  for (const point of points) {
    map.set(dedupeKey(point), point);
  }
  return Array.from(map.values()).sort(
    (a, b) => parseApiTimestampMs(a.timestamp) - parseApiTimestampMs(b.timestamp)
  );
}

/** Trim and dedupe chart points. Set applyRollingWindow=false for fixed history ranges. */
export function trimChartPoints(
  points: ChartDataPoint[],
  options: ChartTrimOptions,
  applyRollingWindow = true,
  referenceNow = Date.now()
): ChartDataPoint[] {
  const sorted = dedupeChartPoints(points);

  if (options.windowMode === 'time' && applyRollingWindow) {
    const windowMs = options.timeWindowMinutes * 60 * 1000;
    const xMin = referenceNow - windowMs;
    return sorted.filter((point) => {
      const t = parseApiTimestampMs(point.timestamp);
      if (Number.isNaN(t)) return false;
      return t >= xMin && t <= referenceNow + LIVE_CLOCK_SKEW_MS;
    });
  }

  if (options.windowMode === 'points') {
    if (sorted.length > options.maxDataPoints) {
      return sorted.slice(-options.maxDataPoints);
    }
  }

  return sorted;
}

export function formatRollingWindowLabel(timeWindowMinutes: number): string {
  if (timeWindowMinutes < 1) {
    return `${(timeWindowMinutes * 60).toFixed(0)} seconds`;
  }
  if (timeWindowMinutes < 2) {
    return `${timeWindowMinutes.toFixed(1)} minutes`;
  }
  return `${timeWindowMinutes.toFixed(0)} minutes`;
}

export function liveFetchPageSize(timeWindowMinutes: number): number {
  const expectedSamples = Math.ceil(timeWindowMinutes * 60 * 1.2);
  return Math.min(500, Math.max(50, expectedSamples));
}

const HISTORY_PAGE_SIZE_CAP = 5000;

export { HISTORY_PAGE_SIZE_CAP };

/** Page size for history API fetch (~1 Hz assumed). Capped to avoid huge responses. */
export function historyPageSize(
  range: '10m' | '1h' | '24h' | 'custom',
  aroundMinutes?: number
): number {
  if (range === '10m') return Math.min(HISTORY_PAGE_SIZE_CAP, 600);
  if (range === '1h') return Math.min(HISTORY_PAGE_SIZE_CAP, 3600);
  if (range === '24h') return HISTORY_PAGE_SIZE_CAP;
  if (range === 'custom' && aroundMinutes != null) {
    return Math.min(HISTORY_PAGE_SIZE_CAP, Math.ceil(aroundMinutes * 2 * 60 * 1.2));
  }
  return 100;
}

/** Y-axis for line chart: zoom to data when sensor min/max is much wider than readings. */
export function computeLineChartYDomain(
  values: number[],
  sensorMin?: number | null,
  sensorMax?: number | null
): [number, number] | undefined {
  if (values.length === 0) {
    if (sensorMin != null && sensorMax != null) {
      return [Number(sensorMin), Number(sensorMax)];
    }
    return undefined;
  }

  let dataMin = Math.min(...values);
  let dataMax = Math.max(...values);
  if (dataMin === dataMax) {
    dataMin -= 2;
    dataMax += 2;
  }

  const dataSpan = dataMax - dataMin;
  const pad = Math.max(dataSpan * 0.05, 2);
  let yMin = dataMin - pad;
  let yMax = dataMax + pad;

  if (sensorMin != null && sensorMax != null) {
    const sMin = Number(sensorMin);
    const sMax = Number(sensorMax);
    const dataFitsInSensorBand = dataMin >= sMin && dataMax <= sMax;
    const sensorSpan = sMax - sMin;
    if (dataFitsInSensorBand && sensorSpan <= 3 * dataSpan) {
      return [sMin, sMax];
    }
    yMin = Math.min(sMin, yMin);
    yMax = Math.max(sMax, yMax);
    if (yMax <= yMin) {
      return [sMin, sMax];
    }
    return [yMin, yMax];
  }

  return [yMin, yMax];
}

export function pointsInLiveWindow(
  points: ChartDataPoint[],
  windowMinutes: number,
  referenceNow: number
): ChartDataPoint[] {
  const windowMs = windowMinutes * 60 * 1000;
  const xMin = referenceNow - windowMs;
  return points.filter((point) => {
    const t = parseApiTimestampMs(point.timestamp);
    if (Number.isNaN(t)) return false;
    return t >= xMin && t <= referenceNow + LIVE_CLOCK_SKEW_MS;
  });
}

/** True when the window is full enough to scroll (≥2 in-window points, oldest at left edge). */
export function isLiveScrollActive(
  points: ChartDataPoint[],
  windowMinutes: number,
  referenceNow: number
): boolean {
  const inWindow = pointsInLiveWindow(points, windowMinutes, referenceNow);
  if (inWindow.length < 2) return false;
  const windowMs = windowMinutes * 60 * 1000;
  const xMin = referenceNow - windowMs;
  const oldest = parseApiTimestampMs(inWindow[0].timestamp);
  return oldest <= xMin;
}

export function formatHistoryRangeLabel(
  trendRange: 'live' | '10m' | '1h' | '24h' | 'custom',
  aroundMinutes?: number
): string {
  if (trendRange === 'custom' && aroundMinutes != null) {
    return `centered +/- ${aroundMinutes}m`;
  }
  if (trendRange === 'live') return 'live';
  return trendRange;
}

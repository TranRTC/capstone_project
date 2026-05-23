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
    (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
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
      const t = new Date(point.timestamp).getTime();
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
    const sensorSpan = sMax - sMin;
    if (sensorSpan <= 3 * dataSpan) {
      return [sMin, sMax];
    }
    yMin = Math.max(sMin, yMin);
    yMax = Math.min(sMax, yMax);
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
    const t = new Date(point.timestamp).getTime();
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
  const oldest = new Date(inWindow[0].timestamp).getTime();
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

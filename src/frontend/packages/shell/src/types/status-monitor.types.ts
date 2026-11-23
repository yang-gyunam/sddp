/** Observable service or check, identified across snapshots by a stable id. */
export interface StatusReading {
  id: string;
  name: string;
  group: string;
  check: string;
  status: string;
  message: string | null;
  responseTimeMs: number | null;
  required: boolean;
}

export interface StatusReport {
  checkedAt: string;
  status: string;
  services: StatusReading[];
}

/** A null report records an observation gap at the client. */
export interface StatusObservation {
  at: string;
  report: StatusReport | null;
}

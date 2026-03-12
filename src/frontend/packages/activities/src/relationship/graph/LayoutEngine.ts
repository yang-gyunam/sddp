/**
 * Layout Engine Interface
 * Abstraction for different graph layout strategies (force, tree, radial)
 */

import type { SimulationNode, SimulationLink, ForceGraphConfig } from '../types';

export interface LayoutEngineCallbacks {
  onTick: (nodes: SimulationNode[], links: SimulationLink[]) => void;
  onEnd?: () => void;
}

export interface LayoutEngine {
  /** Initialize the layout with nodes and links */
  init(
    nodes: SimulationNode[],
    links: SimulationLink[],
    width: number,
    height: number,
    config: ForceGraphConfig
  ): void;

  /** Start or restart the simulation */
  start(): void;

  /** Stop the simulation */
  stop(): void;

  /** Set tick callback */
  setCallbacks(callbacks: LayoutEngineCallbacks): void;

  /** Begin dragging a node */
  dragStart(node: SimulationNode): void;

  /** Update dragged node position */
  dragMove(node: SimulationNode, x: number, y: number): void;

  /** End dragging a node */
  dragEnd(node: SimulationNode): void;

  /** Update configuration */
  updateConfig(config: Partial<ForceGraphConfig>): void;

  /** Clean up resources */
  destroy(): void;
}

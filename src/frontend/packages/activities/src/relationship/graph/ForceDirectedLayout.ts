/**
 * Force-Directed Layout Engine
 * Wraps d3-force simulation with a clean interface for Svelte integration
 */

import {
  forceSimulation,
  forceLink,
  forceManyBody,
  forceCenter,
  forceCollide,
  type Simulation,
} from 'd3-force';
import type { SimulationNode, SimulationLink, ForceGraphConfig } from '../types';
import type { LayoutEngine, LayoutEngineCallbacks } from './LayoutEngine';

export class ForceDirectedLayout implements LayoutEngine {
  private simulation: Simulation<SimulationNode, SimulationLink> | null = null;
  private callbacks: LayoutEngineCallbacks | null = null;
  private nodes: SimulationNode[] = [];
  private links: SimulationLink[] = [];

  setCallbacks(callbacks: LayoutEngineCallbacks): void {
    this.callbacks = callbacks;
  }

  init(
    nodes: SimulationNode[],
    links: SimulationLink[],
    width: number,
    height: number,
    config: ForceGraphConfig
  ): void {
    this.destroy();
    this.nodes = nodes;
    this.links = links;

    this.simulation = forceSimulation<SimulationNode>(nodes)
      .force(
        'link',
        forceLink<SimulationNode, SimulationLink>(links)
          .id((d) => d.id)
          .distance(config.linkDistance)
      )
      .force('charge', forceManyBody<SimulationNode>().strength(config.chargeStrength))
      .force('center', forceCenter<SimulationNode>(width / 2, height / 2).strength(config.centerStrength))
      .force('collide', forceCollide<SimulationNode>(config.collideRadius))
      .alphaDecay(config.alphaDecay)
      .velocityDecay(config.velocityDecay)
      .on('tick', () => {
        this.callbacks?.onTick(this.nodes, this.links);
      })
      .on('end', () => {
        this.callbacks?.onEnd?.();
      });
  }

  start(): void {
    this.simulation?.alpha(1).restart();
  }

  stop(): void {
    this.simulation?.stop();
  }

  dragStart(node: SimulationNode): void {
    this.simulation?.alphaTarget(0.3).restart();
    node.fx = node.x;
    node.fy = node.y;
  }

  dragMove(_node: SimulationNode, x: number, y: number): void {
    _node.fx = x;
    _node.fy = y;
  }

  dragEnd(node: SimulationNode): void {
    this.simulation?.alphaTarget(0);
    node.fx = null;
    node.fy = null;
  }

  updateConfig(config: Partial<ForceGraphConfig>): void {
    if (!this.simulation) return;

    if (config.chargeStrength !== undefined) {
      this.simulation.force('charge', forceManyBody<SimulationNode>().strength(config.chargeStrength));
    }
    if (config.linkDistance !== undefined) {
      this.simulation.force(
        'link',
        forceLink<SimulationNode, SimulationLink>(this.links)
          .id((d) => d.id)
          .distance(config.linkDistance)
      );
    }
    if (config.collideRadius !== undefined) {
      this.simulation.force('collide', forceCollide<SimulationNode>(config.collideRadius));
    }

    this.simulation.alpha(0.5).restart();
  }

  destroy(): void {
    if (this.simulation) {
      this.simulation.stop();
      this.simulation = null;
    }
    this.nodes = [];
    this.links = [];
  }
}

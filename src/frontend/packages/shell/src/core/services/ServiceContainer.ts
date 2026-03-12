/**
 * ServiceContainer - Dependency Injection Container
 * Provides a simple DI container with singleton and transient lifecycle support
 */

export type ServiceFactory<T> = () => T;

export interface ServiceDescriptor<T> {
  factory: ServiceFactory<T>;
  lifecycle: 'singleton' | 'transient';
  instance?: T;
}

export class ServiceContainer {
  private services = new Map<string, ServiceDescriptor<unknown>>();
  private static instance: ServiceContainer | null = null;

  /**
   * Get the singleton instance of the ServiceContainer
   */
  static getInstance(): ServiceContainer {
    if (!ServiceContainer.instance) {
      ServiceContainer.instance = new ServiceContainer();
    }
    return ServiceContainer.instance;
  }

  /**
   * Reset the container (useful for testing)
   */
  static reset(): void {
    ServiceContainer.instance = null;
  }

  /**
   * Register a singleton service
   * The same instance is returned for every resolve call
   */
  registerSingleton<T>(key: string, factory: ServiceFactory<T>): this {
    this.services.set(key, {
      factory,
      lifecycle: 'singleton',
    });
    return this;
  }

  /**
   * Register a transient service
   * A new instance is created for every resolve call
   */
  registerTransient<T>(key: string, factory: ServiceFactory<T>): this {
    this.services.set(key, {
      factory,
      lifecycle: 'transient',
    });
    return this;
  }

  /**
   * Register a service instance directly (singleton)
   */
  registerInstance<T>(key: string, instance: T): this {
    this.services.set(key, {
      factory: () => instance,
      lifecycle: 'singleton',
      instance,
    });
    return this;
  }

  /**
   * Resolve a service by key
   * @throws Error if service is not registered
   */
  resolve<T>(key: string): T {
    const descriptor = this.services.get(key) as ServiceDescriptor<T> | undefined;

    if (!descriptor) {
      throw new Error(`Service '${key}' is not registered`);
    }

    if (descriptor.lifecycle === 'singleton') {
      if (descriptor.instance === undefined) {
        descriptor.instance = descriptor.factory();
      }
      return descriptor.instance;
    }

    return descriptor.factory();
  }

  /**
   * Try to resolve a service, returning undefined if not found
   */
  tryResolve<T>(key: string): T | undefined {
    try {
      return this.resolve<T>(key);
    } catch {
      return undefined;
    }
  }

  /**
   * Check if a service is registered
   */
  has(key: string): boolean {
    return this.services.has(key);
  }

  /**
   * Unregister a service
   */
  unregister(key: string): boolean {
    return this.services.delete(key);
  }

  /**
   * Clear all registered services
   */
  clear(): void {
    this.services.clear();
  }

  /**
   * Get all registered service keys
   */
  getKeys(): string[] {
    return Array.from(this.services.keys());
  }
}

// Export a default container instance
export const container = ServiceContainer.getInstance();

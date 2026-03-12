/**
 * useAppBootstrap — Phase 4
 * Manages app initialization lifecycle: error handler, visibility sync,
 * shortcuts, event listeners, router, deep link redirect
 */

import {
  truncate,
  capitalize,
  initErrorHandler,
  destroyErrorHandler,
  initVisibilitySync,
  destroyVisibilitySync,
  RouterService,
} from '@sddp/shell';
import { setupTabKeyboardShortcuts, commandPalette } from '@sddp/shell/stores';
import { getAuthState } from '@sddp/shell/auth/stores';
import { getMyNotifications, setNotifications } from '@sddp/activities/dashboard';
import { initShortcuts } from '../lib/shortcuts';

type ReferenceNavigateDetail = {
  type: 'spec' | 'conversation' | 'requirement' | 'glossary';
  id: string;
  label?: string;
};

type DirectMessageConcludedDetail = {
  conversationId: string;
  origin?: string;
};

type OpenDmFromNotificationDetail = {
  conversationId: string;
  projectId?: string | null;
  participantName?: string;
  channelStatus?: 'Active' | 'Concluded' | null;
};

export interface AppBootstrapParams {
  onOpenTabFromPath: (path: string) => boolean;
  onOpenDirectMessageByContext: (
    conversationId: string,
    participantName: string,
    projectId?: string | null,
    channelStatus?: 'Active' | 'Concluded' | null
  ) => Promise<void>;
  onHandleDMConcludedById: (id: string) => void;
  onOpenTabWithComponent: (
    title: string,
    icon: string,
    path: string,
    menuId: string,
    options?: { meta?: string; additionalProps?: Record<string, unknown> }
  ) => void;
}

export function useAppBootstrap(params: AppBootstrapParams) {
  let quickSwitcherVisible = $state(false);

  // --- URL helpers ---

  function getReturnUrlFromQuery(): string | null {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('returnUrl');
  }

  function setReturnUrlInQuery(path: string): void {
    const encoded = encodeURIComponent(path);
    window.history.replaceState(null, '', `/?returnUrl=${encoded}`);
  }

  function clearReturnUrlFromQuery(): void {
    if (window.location.search.includes('returnUrl')) {
      window.history.replaceState(null, '', '/');
    }
  }

  function navigateToReturnUrl() {
    const path = getReturnUrlFromQuery();
    if (!path) return;
    clearReturnUrlFromQuery();
    params.onOpenTabFromPath(path);
  }

  function handleDeepLinkRedirect() {
    const initialPath = window.location.pathname;
    if (initialPath && initialPath !== '/') {
      setReturnUrlInQuery(initialPath);
    }
  }

  // --- Event handlers ---

  function handleCommandPaletteShortcut(e: KeyboardEvent) {
    if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'P') {
      e.preventDefault();
      commandPalette.toggle();
    }
  }

  function handleQuickSwitcherShortcut(e: KeyboardEvent) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'Tab') {
      e.preventDefault();
      quickSwitcherVisible = true;
    }
  }

  function handleReferenceNavigate(event: Event) {
    const detail = (event as CustomEvent<ReferenceNavigateDetail>).detail;
    if (!detail?.id || !detail?.type) return;

    const { id, type, label } = detail;
    const shortId = truncate(id, 8, '…');
    const rawTitle = label || `${capitalize(type)} ${shortId}`;
    const title = `${rawTitle} (view)`;
    const readonlyProps = { additionalProps: { readonly: true } };

    switch (type) {
      case 'spec':
        params.onOpenTabWithComponent(
          title,
          'file-signature',
          `/spec/${id}`,
          `spec-${id}`,
          readonlyProps
        );
        break;
      case 'conversation':
        params.onOpenTabWithComponent(
          title,
          'message-square',
          `/conversation/${id}`,
          `conversation-${id}`,
          readonlyProps
        );
        break;
      case 'requirement':
        params.onOpenTabWithComponent(
          title,
          'clipboard-list',
          `/requirement/${id}`,
          `requirement-${id}`,
          readonlyProps
        );
        break;
      case 'glossary':
        params.onOpenTabWithComponent(
          title,
          'book-open',
          `/glossary/${id}`,
          `glossary-${id}`,
          readonlyProps
        );
        break;
    }
  }

  function handleDirectMessageConcluded(event: Event) {
    const detail = (event as CustomEvent<DirectMessageConcludedDetail>).detail;
    if (!detail?.conversationId) return;
    if (detail.origin === 'app-sync' || detail.origin === 'self-leave') return;
    params.onHandleDMConcludedById(detail.conversationId);
  }

  function handleOpenDmFromNotification(event: Event) {
    const detail = (event as CustomEvent<OpenDmFromNotificationDetail>).detail;
    if (!detail?.conversationId) return;
    void params.onOpenDirectMessageByContext(
      detail.conversationId,
      detail.participantName ?? 'Direct Message',
      detail.projectId ?? '',
      detail.channelStatus ?? 'Active'
    );
  }

  // --- Lifecycle ---

  function setupLifecycle(): () => void {
    initErrorHandler({ logToConsole: true, showPanelOnError: false });
    initVisibilitySync({
      onRefreshNotifications: async () => {
        const tenantId = getAuthState().user?.tenantId;
        if (!tenantId) return;
        try {
          const data = await getMyNotifications(tenantId, 1, 50);
          setNotifications(data.notifications, data.unreadCount);
        } catch {
          // Silently ignore — network errors are handled elsewhere
        }
      },
    });

    return () => {
      destroyErrorHandler();
      destroyVisibilitySync();
    };
  }

  function initialize(): () => void {
    const cleanupShortcuts = initShortcuts();
    const cleanupTabShortcuts = setupTabKeyboardShortcuts();

    // Command palette shortcut
    document.addEventListener('keydown', handleCommandPaletteShortcut);

    // Window event listeners
    window.addEventListener('sddp:navigate', handleReferenceNavigate as () => void);
    window.addEventListener('sddp:dm-concluded', handleDirectMessageConcluded as () => void);
    window.addEventListener(
      'sddp:open-dm-from-notification',
      handleOpenDmFromNotification as () => void
    );

    // Quick Switcher shortcut (Ctrl+Tab)
    document.addEventListener('keydown', handleQuickSwitcherShortcut);

    // Register path-based navigation handler before initializing router
    RouterService.setNavigateHandler((path) => {
      params.onOpenTabFromPath(path);
    });
    const cleanupRouter = RouterService.initialize();

    return () => {
      cleanupShortcuts();
      cleanupTabShortcuts();
      document.removeEventListener('keydown', handleCommandPaletteShortcut);
      window.removeEventListener('sddp:navigate', handleReferenceNavigate as () => void);
      window.removeEventListener(
        'sddp:dm-concluded',
        handleDirectMessageConcluded as () => void
      );
      window.removeEventListener(
        'sddp:open-dm-from-notification',
        handleOpenDmFromNotification as () => void
      );
      document.removeEventListener('keydown', handleQuickSwitcherShortcut);
      cleanupRouter?.();
    };
  }

  return {
    setupLifecycle,
    initialize,
    navigateToReturnUrl,
    handleDeepLinkRedirect,
    get quickSwitcherVisible() {
      return quickSwitcherVisible;
    },
    set quickSwitcherVisible(v: boolean) {
      quickSwitcherVisible = v;
    },
  };
}

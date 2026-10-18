/** Preserve the reader's bottom position when the viewport or its message content resizes. */
export function keepMessageBottomVisible(node: HTMLElement) {
  // Browser anchoring otherwise moves scrollTop while responsive panels reflow,
  // making a layout adjustment indistinguishable from the reader scrolling away.
  const previousOverflowAnchor = node.style.overflowAnchor;
  node.style.overflowAnchor = 'none';
  let height = node.clientHeight;
  let width = node.clientWidth;
  let contentHeight = node.scrollHeight;
  let top = node.scrollTop;
  let pinned = contentHeight - top - height <= 80;
  let disposed = false;

  function onScroll() {
    if (node.clientHeight === 0) return;
    const sameSize =
      node.clientHeight === height &&
      node.clientWidth === width &&
      node.scrollHeight === contentHeight;
    // A resize can emit scroll first; compare against the previous geometry too.
    pinned =
      node.scrollHeight - node.scrollTop - node.clientHeight <= 80 ||
      (!sameSize && height > 0 && contentHeight - node.scrollTop - height <= 80);
    top = node.scrollTop;
    if (sameSize) contentHeight = node.scrollHeight;
  }

  const observer = new ResizeObserver(() => {
    if (disposed) return;
    const nextHeight = node.clientHeight;
    const nextWidth = node.clientWidth;
    if (nextHeight === height && nextWidth === width && node.scrollHeight === contentHeight) return;
    // User scrolling may already have changed scrollTop while its event is still queued.
    if (node.scrollTop !== top) onScroll();
    height = nextHeight;
    width = nextWidth;
    contentHeight = node.scrollHeight;
    if (pinned && height > 0) node.scrollTop = node.scrollHeight;
    top = node.scrollTop;
    // The reader may have returned to the bottom before this notification arrived.
    if (height > 0) pinned = node.scrollHeight - node.scrollTop - height <= 80;
  });
  node.addEventListener('scroll', onScroll, { passive: true });
  observer.observe(node);
  const children = new Set<Element>();
  function observeMessages() {
    if (disposed) return;
    for (const child of children) {
      if (child.parentElement !== node) {
        observer.unobserve(child);
        children.delete(child);
      }
    }
    for (const child of node.children) {
      if (!children.has(child)) {
        children.add(child);
        observer.observe(child);
      }
    }
  }
  // The viewport's border box stays fixed while a source preview grows inside it.
  const mutations = new MutationObserver(observeMessages);
  mutations.observe(node, { childList: true });
  observeMessages();

  return {
    destroy() {
      disposed = true;
      node.style.overflowAnchor = previousOverflowAnchor;
      mutations.disconnect();
      observer.disconnect();
      children.clear();
      node.removeEventListener('scroll', onScroll);
    },
  };
}

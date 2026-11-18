<script lang="ts">
  interface Props {
    projectId: string;
    members?: { userId: string; displayName: string; role: string }[];
  }
  let { projectId: _projectId, members: _members = [] }: Props = $props();
</script>

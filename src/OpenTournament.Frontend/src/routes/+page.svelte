<script>
    import { PUBLIC_HOSTNAME } from '$env/static/public'
    import HomePage from '$lib/components/homepage/index.js';
    
    const siteName = PUBLIC_HOSTNAME;

    //let { data } = $props();
</script>
<svelte:head>
    <title>{siteName}</title>
    <meta name="description" content="Tournaments and Brackets for competition" />
</svelte:head>
<HomePage></HomePage>

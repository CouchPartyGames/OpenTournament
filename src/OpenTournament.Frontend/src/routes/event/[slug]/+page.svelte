<script>
    import { PUBLIC_HOSTNAME } from '$env/static/public'
    const siteName = PUBLIC_HOSTNAME;
    let { data } = $props();
    import  Details from '$lib/components/details/index.js';
</script>
<svelte:head>
    <title>{siteName}</title>
    <meta name="description" content="Tournaments and Brackets for competition" />
    <meta name="keywords" content="Tournament, Bracket, Competition" />
</svelte:head>
<Details></Details>


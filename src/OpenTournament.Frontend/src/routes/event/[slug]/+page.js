import { error } from '@sveltejs/kit';
import { PUBLIC_BASE_URL } from '$env/static/public';

export const prerender = true;

export const load = async ({ params }) => {
    console.log('slug: ' + params.slug);
    const resp = await fetch(PUBLIC_BASE_URL + '/products/v1');
    const data = await resp.json();
    if (data) {
        console.log(data);
        return { data };
    }
    
    error(404, 'No such product');
};

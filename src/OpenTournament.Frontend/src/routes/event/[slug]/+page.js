import { error } from '@sveltejs/kit';
import { HTTP_PROTOCOL, HTTP_HOST } from '$env/static/public';

export const load = async ({ params }) => {
    console.log('slug: ' + params.slug);
    console.log(HTTP_PROTOCOL + "://" + HTTP_HOST);
    const resp = await fetch('https://dummyjson.com/products');
    const data = await resp.json();
    if (data) {
        console.log(data);
        return { data };
    }
    
    error(404, 'No such product');
};

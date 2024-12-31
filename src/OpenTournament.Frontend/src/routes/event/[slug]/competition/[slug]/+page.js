import { error } from '@sveltejs/kit';

export const prerender = true;

export const ssr = true;
export const load = async ({fetch}) => {
    const resp = await fetch('https://dummyjson.com/products');
    if (resp.status === 200) {
        const data = await resp.json();
        if (data) {
            console.log(data);
            return { data };
        }
    }
    
    error(404, "Not Found");
};

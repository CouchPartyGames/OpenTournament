import { error } from '@sveltejs/kit';

export const load = async ({fetch}) => {
    const resp = await fetch('https://dummyjson.com/products');
    const data = await resp.json();
    if (data) {
        console.log(data);
        return { data };
    }
    
    error(404, "Not Found");
};

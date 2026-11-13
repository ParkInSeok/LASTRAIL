import {mergeConfig} from 'vite';import config from './vite.config';
export default mergeConfig(config,{server:{host:'127.0.0.1',port:5288,strictPort:true,proxy:{'/api':'http://127.0.0.1:4488'},fs:{allow:['D:/Project/LASTRAIL']}},cacheDir:'D:/Project/LASTRAIL/LocalTemp/vite-existing-mount'});

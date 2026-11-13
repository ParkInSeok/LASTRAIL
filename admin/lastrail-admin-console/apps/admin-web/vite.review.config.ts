import {mergeConfig} from 'vite';import config from './vite.config';
export default mergeConfig(config,{server:{host:'127.0.0.1',port:5278,strictPort:true,proxy:{'/api':'http://127.0.0.1:4478','/__qa':'http://127.0.0.1:4478'},fs:{allow:['D:/Project/LASTRAIL']}},cacheDir:'D:/Project/LASTRAIL/LocalTemp/vite-draft-review'});

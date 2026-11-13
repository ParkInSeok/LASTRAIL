import test from 'node:test';
import assert from 'node:assert/strict';
import {searchFaq} from '../src/faq-search.mjs';
const entries=[['광고 OFF 안내','추가 보상 조건'],['계정 문의','결제 정보 보호']];
test('FAQ search matches questions and answers, all terms, case and whitespace',()=>{
 assert.deepEqual(searchFaq(entries,'  광고   off '),[0]);
 assert.deepEqual(searchFaq(entries,'결제'),[1]);
 assert.deepEqual(searchFaq(entries,'광고 결제'),[]);
 assert.deepEqual(searchFaq(entries,'ＯＦＦ'),[0]);
 assert.deepEqual(searchFaq(entries,'   '),[0,1]);
 assert.deepEqual(searchFaq(entries,'<script>'),[]);
 assert.deepEqual(entries,[['광고 OFF 안내','추가 보상 조건'],['계정 문의','결제 정보 보호']]);
});

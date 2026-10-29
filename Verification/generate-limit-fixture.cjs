const fs = require('fs'), lz = require('../H5MotaSaveDecoder/JavaScript/lz-string.js');
// For an all-A input LZString's dictionary entries are A, AA, AAA, ...
// Emit those known runs without constructing/hashing 64 MiB of growing substrings.
function repeatedA(count) {
 const alphabet='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
 let bits=2, enlarge=2, value=0, position=0, out='', first=true;
 function write(n,v) { for(let i=0;i<n;i++){value=(value<<1)|(v&1);v>>=1;if(++position===6){out+=alphabet[value];value=position=0;}} }
 function grow(){ if(--enlarge===0){enlarge=1<<bits;bits++;} }
 function emit(length){if(first){write(bits,0);write(8,65);grow();first=false;}else write(bits,length+2);grow();}
 for(let length=1;count>length;length++){emit(length);count-=length;}
 if(count)emit(count);
 write(bits,2);
 do{value<<=1;position++;}while(position<6);
 out+=alphabet[value];
 while(out.length%4)out+='=';
 return out;
}
for (const count of [1,2,3,4,9,100,1000,65536]) {
 if (repeatedA(count)!==lz.compressToBase64('A'.repeat(count))) throw new Error('Reference mismatch '+count);
}
const encoded=repeatedA(64*1024*1024+1);
fs.writeFileSync(require('path').join(__dirname,'over-limit.lz.txt'),encoded);
console.log('Oversize fixture generated; 8 independent upstream comparisons passed; encoded chars='+encoded.length);

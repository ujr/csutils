% Illustration about SparseBitSet
% ujr/2016-09-19, -09-26

input boxes

verbatimtex \parindent=0pt
\def\stk#1#2{$\displaystyle{\matrix{\hbox{#1}\cr\hbox{#2}\cr}}$} etex

vardef drawnil expr p =
  save n,e,d; n = length p; pair e; numeric d;
  draw p;
  e = point n of p;
  d = angle direction n of p;
  draw (left--right) scaled 2pt rotated (75+d) shifted e
    withpen pencircle scaled 1pt;
enddef;

beginfig(1);
interim defaultscale:=0.8;
% The catalog:
boxjoin(a.sw=b.nw;a.se=b.ne);
interim defaultdy:=4pt; interim defaultdx:=24pt;
boxit.ca0(); boxit.ca1(); boxit.ca2(); boxit.ca3();
boxit.ca4(); boxit.ca5(); boxit.ca6(); boxit.ca7();
boxit.ca8(btex $\vdots\strut$ etex); boxit.ca9();
drawboxed(ca0,ca1,ca2,ca3,ca4,ca5,ca6,ca7,ca8,ca9);
label.top(btex Catalog:\strut etex, ca0.n+(0,1pc));
label.urt("63", ca0.nw); label.ulft("0", ca0.ne);
label.lft("0", ca0.w); label.lft("1", ca1.w); label.lft("2", ca2.w);
label.lft("3", ca3.w); label.lft("4", ca4.w); label.lft("5", ca5.w);
label.lft(btex $\lceil n/4096\rceil-1$ etex, ca9.w);
% The chunks:
boxjoin(a.sw=b.nw;a.se=b.ne);
boxit.cu0(); boxit.cu1(); boxit.cu2(); boxit.cu3();
boxit.cu4(); boxit.cu5(); boxit.cu6(); boxit.cu7();
boxit.cu8(btex $\vdots\strut$ etex); boxit.cu9();
ca0.e+5cm*right=cu0.w;
drawboxed(cu0,cu1,cu2,cu3,cu4,cu5,cu6,cu7,cu8,cu9);
label.top(btex Chunks:\strut etex, cu0.n+(0,1pc));
label.lft("0", cu0.w); label.lft("1", cu1.w); label.lft("2", cu2.w);
label.lft("3", cu3.w); label.lft("4", cu4.w); label.lft("5", cu5.w);
label.lft(btex $\lceil n/4096\rceil-1$ etex, cu9.w);
% Catalog word flags:
interim defaultdx:=4pt; defaultdy:=2pt;
boxjoin(a.ne=b.nw;a.se=b.sw);
boxit.fhi(btex $\;\cdots\;$ etex);
boxit.fk();
boxit.flo(btex $\;\cdots\;$ etex);
fhi.w = ca4.e+1cm*right;
drawboxed(fhi,fk,flo);
label.urt("63", fhi.nw); label.top(btex $k$ etex, fk.n);
label.ulft("0", flo.ne); label.lft(btex $j:$ etex, fhi.w);
label.top(btex ${\hbox{The bits in a}\atop\hbox{catalog word:}}$ etex,
          fk.n+1pc*up);
drawarrow fk.c--(fk.c+1.2pc*down);
label.bot(btex \vbox{\hsize=3cm\hfil 1 iff word $k$\hfil\break
               \null\hfil in chunk $j$\hfil\break
               \null\hfil is non-zero} etex, fk.c+1.2pc*down);
% Two chunk arrays:
drawnil 2/3[cu0.c,cu0.e]--(cu0.e+1pc*right);
drawnil 2/3[cu1.c,cu1.e]--(cu1.e+1pc*right);
drawnil 2/3[cu3.c,cu3.e]--(cu3.e+1pc*right);
drawnil 2/3[cu5.c,cu5.e]--(cu5.e+.9pc*right);
boxjoin(a.sw=b.nw;a.se=b.ne);
interim defaultdy:=4pt; interim defaultdx:=24pt;
boxit.a0(); boxit.a1(); boxit.a2();
boxit.a3(btex $\vdots\strut$ etex);
cu0.e+3.5cm*right=a0.w;
drawboxed(a0,a1,a2,a3);
label.urt("63", a0.nw); label.ulft("0", a0.ne);
drawarrow 2/3[cu2.c,cu2.e]{right}..{right}a0.nw;
boxjoin(a.sw=b.nw;a.se=b.ne);
boxit.b0(); boxit.b1();
boxit.b2(btex $\vdots\strut$ etex);
cu6.e+0.8cm*right=b0.w;
drawboxed(b0,b1,b2);
label.urt("63", b0.nw); label.ulft("0", b0.ne);
drawarrow 2/3[cu4.c,cu4.e]{right}..{dir-65}b0.nw;
label(btex \vbox{\hsize=3cm a chunk is an array
           of at least 1 and at most 64 words
           of 64 bits each} etex, (xpart a0.c,ypart b2.c));
endfig;

beginfig(2); % bit address layout
interim defaultscale:=0.8;
boxjoin(a.ne=b.nw;a.se=b.sw);
boxit.s(btex $\scriptstyle 0$ etex);
boxit.j(btex \hskip4pc etex);
boxit.k(btex \hskip1.5pc etex);
boxit.o(btex \hskip1.5pc etex);
drawboxed(s,j,k,o);
label.lft(btex $i$: etex, s.w);
label.urt("31", s.nw); label.ulft("12", j.ne);
label.ulft("6", k.ne); label.ulft("0", o.ne);
drawarrow o.s--(o.c+1pc*down)--(o.c+1pc*down+1pc*right);
label.rt(btex bit within word etex, o.c+1pc*down+1pc*right);
drawarrow k.s--(k.c+2pc*down)--(k.c+2pc*down+1pc*right);
label.rt(btex $k$ = word within chunk etex, k.c+2pc*down+1pc*right);
drawarrow j.s--(j.c+2pc*down);
label.bot(btex chunk number $j$ etex, j.c+2pc*down);
drawarrow subpath (0,.9) of ((s.c+2pc*down+1pc*left){dir30}..{dir60}s.s)
          dashed evenly scaled .5;
label.lft(btex \stk{sign bit is}{always zero} etex scaled .75,
          s.c+2pc*down+1pc*left);
endfig;

bye

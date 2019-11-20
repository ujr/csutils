verbatimtex
\def\Z{\mathop{\rm Z}}%
etex

% Compute Z address (works only for small values):
vardef Z(expr i,j) = save k, x, y, r, f;
 r = 0; f = 1; x = i; y = j;
 for k = 0 upto 4: % max for MPs numeric range
  if odd x: r := r + f fi; f := f * 2; x := x div 2;
  if odd y: r := r + f fi; f := f * 2; y := y div 2;
 endfor
 r % result
enddef;

beginfig(1)
numeric u, i, j; u=7mm;
numeric xmax, ymax; xmax=19; ymax=19;
% Draw gray Z in background:
draw u*(4,4)--u*(12,4)--u*(4,12)--u*(12,12)
 withcolor .9white withpen pencircle scaled 3.6u;
draw u*(2,2)--u*(6,2)--u*(2,6)--u*(6,6)
 withcolor .8white withpen pencircle scaled 1.8u;
draw u*(1,1)--u*(3,1)--u*(1,3)--u*(3,3)
 withcolor .7white withpen pencircle scaled 0.8u;
draw u*(.5,.5)--u*(1.5,0.5)--u*(0.5,1.5)--u*(1.5,1.5)
 withcolor .6white withpen pencircle scaled 0.5u;
% Draw frame and axes:
drawarrow (-.3u,0)--u*(xmax+1.0,0);
drawarrow (0,-.3u)--u*(0,ymax+.5);
draw u*(-.3,ymax)--u*(xmax,ymax)--u*(xmax,-.3);
undraw u*(xmax-1.5,0) withpen pencircle scaled .2u;
draw (-right--right) scaled .1u rotated 60 shifted (u*xmax-1.6u,0);
draw (-right--right) scaled .1u rotated 60 shifted (u*xmax-1.4u,0);
undraw u*(xmax-2,ymax) withpen pencircle scaled .2u;
draw (-right--right) scaled .1u rotated 60 shifted (u*(xmax-2.1,ymax));
draw (-right--right) scaled .1u rotated 60 shifted (u*(xmax-1.9,ymax));
undraw u*(0,ymax-1.5) withpen pencircle scaled .2u;
draw (-right--right) scaled .1u rotated 30 shifted (0,u*ymax-1.6u);
draw (-right--right) scaled .1u rotated 30 shifted (0,u*ymax-1.4u);
undraw u*(xmax,ymax-2) withpen pencircle scaled .2u;
draw (-right--right) scaled .1u rotated 30 shifted (u*xmax,u*ymax-2.1u);
draw (-right--right) scaled .1u rotated 30 shifted (u*xmax,u*ymax-1.9u);
% Draw (part of) grid:
for i = 1 upto 8: draw u*(i,0)--u*(i,16.5); endfor
for i = 9 upto 16: draw u*(i,0)--u*(i,8.5); endfor
for j = 1 upto 8: draw u*(0,j)--u*(16.5,j); endfor
for j = 9 upto 16: draw u*(0,j)--u*(8.5,j); endfor
draw u*(xmax-1.5,ymax-1)--u*(xmax,ymax-1);
draw u*(xmax-1,ymax-1.5)--u*(xmax-1,ymax);
% Z curve labels:
label.lft(btex $\Z(2^{32}-1,2^{32}-1)=2^{64}-1$ etex, u*(xmax-2,ymax-1.5));
drawarrow u*(xmax-2,ymax-1.5){right}..u*(xmax-.5,ymax-.5);
for i = 0 upto 15: for j = 0 upto 7:
  label(decimal Z(i,j), u*(i+.5,j+.5));
endfor endfor
for i = 0 upto 7: for j = 8 upto 15:
  label(decimal Z(i,j), u*(i+.5,j+.5));
endfor endfor
label(decimal Z(8,8), u*(8.5,8.5));
label(decimal Z(16,0), u*(16.5,0.5));
label(decimal Z(0,16), u*(0.5,16.5));
% Frame labels:
label.rt(btex $x$ etex, u*(xmax+.9,0.2));
label.urt(btex $y$ etex, u*(0.2,ymax+.2));
for i = 0 upto 15: label.bot(decimal i, u*(i+.5,0)); endfor
for j = 0 upto 15: label.lft(decimal j, u*(0,j+.5)); endfor
label.bot(btex $\dots$ etex, u*(16.5,-0.15));
label.bot(btex $2^{32}$ etex, u*(xmax+.5,0));
label.lft(btex $\vdots$ etex, u*(-0.05,16.6));
label.lft(btex $2^{32}$ etex, u*(0,ymax+.4));
% Highlight a few blocks:
draw unitsquare scaled u shifted (7u,11u) withpen pencircle scaled 1.5pt;
draw unitsquare scaled 2u shifted (6u,10u) withpen pencircle scaled 1.5pt;
draw unitsquare scaled 4u shifted (4u,8u) withpen pencircle scaled 1.5pt;
label.rt(btex cell (block at level 0) etex, u*(10,12.0));
label.rt(btex block: start=156, level=1 etex, u*(10,11.0));
label.rt(btex block: start=144, level=2 etex, u*(10,10.0));
drawarrow u*(10,12.0){left}..{left}u*(8,11.5);
drawarrow u*(10,11.0){left}..{left}u*(8,10.5);
drawarrow u*(10,10.0){left}..{left}u*(8,9.5);
endfig;

end

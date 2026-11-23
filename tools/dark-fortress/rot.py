import math, sys
# Unity euler (degrees, ZXY order: q = qy*qx*qz) helpers
def qmul(a, b):
    w1,x1,y1,z1=a; w2,x2,y2,z2=b
    return (w1*w2-x1*x2-y1*y2-z1*z2, w1*x2+x1*w2+y1*z2-z1*y2, w1*y2-x1*z2+y1*w2+z1*x2, w1*z2+x1*y2-y1*x2+z1*w2)
def axis(ax, deg):
    h=math.radians(deg)/2; s=math.sin(h)
    return (math.cos(h), s if ax=='x' else 0, s if ax=='y' else 0, s if ax=='z' else 0)
def e2q(e):
    return qmul(qmul(axis('y',e[1]),axis('x',e[0])),axis('z',e[2]))
def q2e(q):
    w,x,y,z=q
    # rotation matrix
    m = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
         [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
         [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]]
    sx = -m[1][2]
    sx = max(-1,min(1,sx))
    ex = math.asin(sx)
    if abs(sx) < 0.9999:
        ey = math.atan2(m[0][2], m[2][2]); ez = math.atan2(m[1][0], m[1][1])
    else:
        ey = math.atan2(-m[2][0], m[0][0]); ez = 0
    return tuple(round(math.degrees(a) % 360, 3) for a in (ex, ey, ez))
def compose(base, local):
    """base euler then extra rotation in the model's own space"""
    return q2e(qmul(e2q(base), e2q(local)))
if __name__ == '__main__':
    b = tuple(float(v) for v in sys.argv[1].split(','))
    l = tuple(float(v) for v in sys.argv[2].split(','))
    print(','.join(str(v) for v in compose(b, l)))

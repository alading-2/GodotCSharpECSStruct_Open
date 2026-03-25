import { Position } from "./Position";
import { Vector } from "./Vector";

export class Bezier {
  position: Position[];
  length: number;
  vector: Vector[];

  constructor(positions: Position[]) {
    this.position = positions;
    this.length = positions.length;
  }

  /**
   * 获取t值的点
   * @param t t
   * @returns 点
   */
  value(t: number): Position {
    let x = 0;
    let y = 0;
    let z = 0;
    //公式系数
    let coefficient = PascalTriangle.GetRow(this.length);
    //贝塞尔阶数比点数少1
    for (let i = 0; i < this.length; i++) {
      let xs = (t ^ i) * ((1 - t) ^ (this.length - 1 - i)) * coefficient[i];
      x += xs * this.position[i].X;
      y += xs * this.position[i].Y;
      z += xs * this.position[i].Z;
    }

    return new Position(x, y, z);
  }

  /**
   * 获取t值的切线方向
   * @param t t
   * @returns Vector
   */
  face(t: number): Vector {
    let x = 0;
    let y = 0;
    let z = 0;
    //贝塞尔阶数比点数少1，向量个数也比点个数少1
    let len = this.length - 1;
    if (!this.vector) {
      this.vector = [];
      for (let i = 0; i < len; i++) {
        this.vector[i] = this.position[i + 1] - this.position[i];
      }
    }

    let yhxs = PascalTriangle.GetRow(len);

    for (let i = 0; i < len; i++) {
      let xs = (t ^ i) * ((1 - t) ^ (this.length - i - 1)) * yhxs[i];
      x += xs * this.vector[i].X;
      y += xs * this.vector[i].Y;
      z += xs * this.vector[i].Z;
    }

    return new Vector(x, y, z);
  }
}

//杨辉三角
export class PascalTriangle {
  static triangleData: number[][] = [];
  //游戏初始化时运行顺序0-100
  private static readonly ONINIT_ORDER = 0;
  //游戏初始化时运行，创建50行杨辉三角
  private static onInit() {
    for (let i = 0; i < 50; i++) {
      let num = i + 1;
      PascalTriangle.triangleData[num] = [];
      for (let j = 0; j < num; j++) {
        if (j == 0 || j == i) {
          PascalTriangle.triangleData[num][j] = 1;
        } else {
          PascalTriangle.triangleData[num][j] =
            PascalTriangle.triangleData[num - 1][j] + PascalTriangle.triangleData[num - 1][j - 1];
        }
      }
    }
  }

  static GetRow(index: number) {
    return PascalTriangle.triangleData[index];
  }
}

class VerticalSliceNode {
    constructor(id, text, link, nodeTypeId) {
        this.id = id;
        this.text = text;
        this.link = link;
        this.nodeTypeId = nodeTypeId
        this.x = 0;
        this.y = 0;
        this.height = 0;
        this.width = 0;
        this.textX = 0;
        this.textY = 0;
        this.textWidth = 0;
        this.textHeight = 0;
        this.calculateSize();
    }

    getFillStyle() {
        switch (this.nodeTypeId) {
            case 0:
                return "#777700";
            case 1:
                return "#AAAAFF";
            case 2:
                return "#AA3333";
            case 3:
                return "#AAAA33";
            case 4:
                return "#AA2233";
            case 5:
                return "#33AAAA";
            case 6:
                return "#77AA99"
            default:
                return "#777700";

        }
    }
    getTopLeftCorner() {
        return {x: this.x, y: this.y}
    }

    getTopRightCorner() {
        return {x: this.x + this.width, y: this.y}
    }

    getBottomLeftCorner() {
        return {x: this.x, y: this.y + this.height}
    }

    getBottomRightCorner() {
        return {x: this.x + this.width, y: this.y + this.height}
    }

    getLeftBorderMidpoint() {
        return {x: this.x, y: this.y + this.height / 2.0}
    }

    getRightBorderMidpoint() {
        return {x: this.x + this.width, y: this.y + this.height / 2.0}
    }

    getTopBorderMidpoint() {
        return {x: this.x + this.width / 2.0, y: this.y}
    }

    getBottomBorderMidpoint() {
        return {x: this.x + this.width / 2.0, y: this.y + this.height}
    }

    getLeft() { return this.x; }

    getTextLeft() { return this.textX; }
    getRight() { return this.x + this.width; }

    getTextRight() { return this.textX + this.textWidth; }
    getTop() { return this.y; }

    getTextTop() { return this.textY - this.textHeight; }
    getBottom() { return this.y + this.height; }

    getTextBottom() { return this.textY; }

    textContainsPoint(x, y) {
        return x >= this.getTextLeft() && x <= this.getTextRight() && y >= this.getTextTop() && y <= this.getTextBottom();
    }

    calculateSize() {
        // Measure text size using a temporary canvas
        const tempCanvas = document.createElement('canvas');
        const context = tempCanvas.getContext('2d');
        context.font = '16px Arial';
        const metrics = context.measureText(this.text);
        this.width = metrics.width + 20; // Add some padding
        this.height = metrics.fontBoundingBoxAscent + 40; // Add some padding
        this.textWidth = metrics.width;
        this.textHeight = metrics.fontBoundingBoxAscent;
    }

    render(ctx, x, y) {
        this.calculateSize();
        this.x = x;
        this.y = y;
        ctx.beginPath();
        ctx.roundRect(this.x, this.y, this.width, this.height, [5]);
        ctx.fillStyle = this.getFillStyle();
        ctx.fill();
        ctx.stroke();

        ctx.font = '16px Arial';
        
        this.textX = this.x + (this.width - this.textWidth) / 2;
        this.textY = this.y + (this.height + this.textHeight) / 2;
        
        
        // Draw the text
        if(this.link && this.link !== "") {
            ctx.fillStyle = "blue";
            ctx.textDecoration = "underline";
        }
        else {
            ctx.fillStyle = 'black';
        }
        ctx.fillText(this.text, this.textX, this.textY);

    }
}
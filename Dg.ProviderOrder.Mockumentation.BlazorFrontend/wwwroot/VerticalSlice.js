class GraphVerticalSlice {

    constructor() {
        this.id = "";
        this.primaryAdapterTuples = new Array(0).fill(null).map(() => new PrimaryAdapterTuple(null))
        this.primaryPort = null;
        this.nServiceBusPayloads = [];
        this.nServiceBusViaOutboxTuples = [];
        this.kafkaPayloads = [];
        this.kafkaViaOutboxTuples = [];
        this.webApiServiceClientCalls = [];
        this.verticalSliceCalls = [];
        this.x = 0;
        this.y = 0;
        this.height = 0;
        this.width = 0;
        this.renderBound = false;
        this.requiresRender = false;
        this.lastDragCoords = undefined
        this.hoveringOverLink = false;
    }

    findOutgoingPayloadNode(id) {
        let result = this.nServiceBusPayloads.find(e => e.id === id);
        if (result) {
            return result;
        }
        result = this.nServiceBusViaOutboxTuples.find(e => e.nServiceBusPayload.id === id);
        if (result) {
            return result.nServiceBusPayload;
        }
        result = this.kafkaPayloads.find(e => e.id === id);
        if (result) {
            return result;
        }
        result = this.kafkaViaOutboxTuples.find(e => e.kafkaPayload.id === id);
        if (result) {
            return result.kafkaPayload;
        }
    }

    findIncomingPayloadNode(id) {
        let result = this.primaryAdapterTuples.find(e => e.payload.id === id);
        return result.primaryAdapter;
    }

    getLeft() {
        return this.x;
    }

    getRight() {
        return this.x + this.width;
    }

    getTop() {
        return this.y;
    }

    getBottom() {
        return this.y + this.height;
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

    containsPoint(x, y) {
        return x >= this.getLeft() && x <= this.getRight() && y >= this.getTop() && y <= this.getBottom();
    }

    onMouseDown(e) {
        let canvasCoords = window.toCanvasCoordinates(e.clientX, e.clientY);
        this.lastDragCoords = {x: e.clientX, y: e.clientY};
        let isLinkClick = this.primaryPort.link && this.primaryPort.link !== "" && this.primaryPort.textContainsPoint(canvasCoords.x, canvasCoords.y);
        if (isLinkClick) {
            this.lastDragCoords = undefined;
            window.open(this.primaryPort.link, "_blank");
            return true;
        }
        if (this.containsPoint(canvasCoords.x, canvasCoords.y)) {
            this.lastDragCoords = canvasCoords;
        } else {
            this.lastDragCoords = undefined;
        }
        return this.lastDragCoords !== undefined;
    }

    onMouseUp(e) {
        this.lastDragCoords = undefined;
    }

    onMouseMove(e) {

        if (this.lastDragCoords !== undefined) {
            let canvasCoords = window.toCanvasCoordinates(e.clientX, e.clientY);
            let offset = {x: canvasCoords.x - this.lastDragCoords.x, y: canvasCoords.y - this.lastDragCoords.y};
            this.x += offset.x;
            this.y += offset.y;
            this.lastDragCoords = canvasCoords;
            this.requiresRender = true;
        } else {
            let canvasCoords = window.toCanvasCoordinates(e.clientX, e.clientY);
            let contains = this.containsPoint(canvasCoords.x, canvasCoords.y);

            if (contains !== this.renderBound) {
                this.renderBound = contains;
                this.requiresRender = true;
            } else {
                let hasLink = this.primaryPort.link && this.primaryPort.link !== "";
                if (this.primaryPort.link && this.primaryPort.link !== "" && this.primaryPort.textContainsPoint(canvasCoords.x, canvasCoords.y)) {

                    this.hoveringOverLink = true;
                    console.log("hovering over link");
                    window.canvas.style.cursor = 'pointer';
                } else if (this.hoveringOverLink) {
                    this.hoveringOverLink = false;
                    console.log("hovering over link no more")
                    window.canvas.style.cursor = 'default';
                }
            }
        }
    }

    render(renderDetailed = true) {
        if (renderDetailed) {
            this.renderDetailed();
        } else {
            this.renderOnlyBox()
        }
    }

    renderOnlyBox() {
        let ctx = window.canvasContext;
        const tempCanvas = document.createElement('canvas');
        const context = tempCanvas.getContext('2d');
        context.font = '16px Arial';
        const metrics = context.measureText(this.primaryPort.text);
        this.width = metrics.width + 20; // Add some padding
        this.height = metrics.fontBoundingBoxAscent + 40; // Add some padding
        let textWidth = metrics.width;
        let textHeight = metrics.fontBoundingBoxAscent;
        ctx.beginPath();
        ctx.roundRect(this.x, this.y, this.width, this.height, [5]);
        ctx.fillStyle = "#AAAAFF";
        ctx.fill();
        ctx.stroke();
        let textX = this.x + (this.width - textWidth) / 2;
        let textY = this.y + (this.height + textHeight) / 2;
        // Draw the text
        ctx.fillStyle = 'black';
        ctx.fillText(this.primaryPort.text, textX, textY);

    }

    renderDetailed() {
        const columnWidth = 800;
        let columnIndex = 0;

        let currentX = this.x;
        let currentY = this.y;
        let ctx = window.canvasContext;
        let maxRight = currentX;
        let maxBottom = currentY;

        this.primaryAdapterTuples.forEach(primaryAdapterTuple => {
            primaryAdapterTuple.primaryAdapter.render(ctx, currentX, currentY);
            currentY = primaryAdapterTuple.primaryAdapter.getBottom() + 20;
            if (primaryAdapterTuple.primaryAdapter.getBottom() > maxBottom) {
                maxBottom = primaryAdapterTuple.primaryAdapter.getBottom();
            }
        });

        columnIndex++;
        currentX = this.x + columnIndex * columnWidth;
        currentY = this.primaryAdapterTuples.length > 1
            ? ((this.y + maxBottom) * 0.5) - (this.primaryPort.height * 0.5)
            : this.y;


        this.primaryPort.render(ctx, currentX, currentY);

        if (this.primaryPort.getRight() > maxRight) {
            maxRight = this.primaryPort.getRight();
        }

        if (this.primaryPort.getBottom() > maxBottom) {
            maxBottom = this.primaryPort.getBottom();
        }

        this.primaryAdapterTuples.forEach(primaryAdapterTuple => {
            renderLine(primaryAdapterTuple.primaryAdapter, this.primaryPort)
        });

        columnIndex++;
        currentX = this.x + columnIndex * columnWidth;

        this.nServiceBusViaOutboxTuples.forEach(nServiceBusViaOutboxTuple => {
            nServiceBusViaOutboxTuple.outboxPayload.render(ctx, currentX, currentY);
            renderLine(this.primaryPort, nServiceBusViaOutboxTuple.outboxPayload);
            columnIndex++;
            currentX = this.x + columnIndex * columnWidth;
            nServiceBusViaOutboxTuple.nServiceBusPayload.render(ctx, currentX, currentY);
            renderLine(nServiceBusViaOutboxTuple.outboxPayload, nServiceBusViaOutboxTuple.nServiceBusPayload);
            currentY = nServiceBusViaOutboxTuple.nServiceBusPayload.getBottom() + 20;
            columnIndex--;
            currentX = this.x + columnIndex * columnWidth;
            if (nServiceBusViaOutboxTuple.nServiceBusPayload.getRight() > maxRight) {
                maxRight = nServiceBusViaOutboxTuple.nServiceBusPayload.getRight();
            }

            if (nServiceBusViaOutboxTuple.nServiceBusPayload.getBottom() > maxBottom) {
                maxBottom = nServiceBusViaOutboxTuple.nServiceBusPayload.getBottom();
            }
        });

        this.nServiceBusPayloads.forEach(nServiceBusPayload => {
            nServiceBusPayload.render(ctx, currentX, currentY);
            renderLine(this.primaryPort, nServiceBusPayload);
            currentY = nServiceBusPayload.getBottom() + 20;

            if (nServiceBusPayload.getRight() > maxRight) {
                maxRight = nServiceBusPayload.getRight();
            }
            if (nServiceBusPayload.getBottom() > maxBottom) {
                maxBottom = nServiceBusPayload.getBottom();
            }
        });

        this.kafkaViaOutboxTuples.forEach(kafkaViaOutboxTuple => {
            kafkaViaOutboxTuple.outboxPayload.render(ctx, currentX, currentY);
            renderLine(this.primaryPort, kafkaViaOutboxTuple.outboxPayload);
            columnIndex++;
            currentX = this.x + columnIndex * columnWidth;
            kafkaViaOutboxTuple.kafkaPayload.render(ctx, currentX, currentY);
            renderLine(kafkaViaOutboxTuple.outboxPayload, kafkaViaOutboxTuple.kafkaPayload);
            currentY = kafkaViaOutboxTuple.kafkaPayload.getBottom() + 20;
            columnIndex--;
            currentX = this.x + columnIndex * columnWidth;

            if (kafkaViaOutboxTuple.kafkaPayload.getRight() > maxRight) {
                maxRight = kafkaViaOutboxTuple.kafkaPayload.getRight();
            }

            if (kafkaViaOutboxTuple.kafkaPayload.getBottom() > maxBottom) {
                maxBottom = kafkaViaOutboxTuple.kafkaPayload.getBottom();
            }
        });

        this.kafkaPayloads.forEach(kafkaPayload => {
            kafkaPayload.render(ctx, currentX, currentY);
            renderLine(this.primaryPort, kafkaPayload);
            currentY = kafkaPayload.getBottom() + 20;

            if (kafkaPayload.getRight() > maxRight) {
                maxRight = kafkaPayload.getRight();
            }
            if (kafkaPayload.getBottom() > maxBottom) {
                maxBottom = kafkaPayload.getBottom();
            }
        });

        this.webApiServiceClientCalls.forEach(webApiServiceClientCall => {
            webApiServiceClientCall.render(ctx, currentX, currentY);
            renderLine(this.primaryPort, webApiServiceClientCall);
            currentY = webApiServiceClientCall.getBottom() + 20;

            if (webApiServiceClientCall.getRight() > maxRight) {
                maxRight = webApiServiceClientCall.getRight();
            }
            if (webApiServiceClientCall.getBottom() > maxBottom) {
                maxBottom = webApiServiceClientCall.getBottom();
            }
        });
        this.width = maxRight - this.x;
        this.height = maxBottom - this.y;

        ctx.lineWidth = 1;
        ctx.strokeStyle = 'black';
        ctx.setLineDash([5, 3]); // [dash length, gap length]
        ctx.strokeRect(this.x, this.y, this.width, this.height); // x, y, width, height
        ctx.setLineDash([]); // Solid lines for future drawings
        if (this.renderBound) {
            ctx.lineWidth = 2;
            ctx.strokeStyle = 'blue';
            ctx.strokeRect(this.x, this.y, this.width, this.height); // x, y, width, height

            ctx.strokeStyle = 'black';
            ctx.lineWidth = 1;
        }

        this.requiresRender = false;
    }
}
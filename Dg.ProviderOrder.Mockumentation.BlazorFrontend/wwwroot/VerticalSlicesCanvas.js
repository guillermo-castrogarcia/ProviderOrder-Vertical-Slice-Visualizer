const canvasId = "verticalSlicesCanvas";

window.canvasState = {
    scale: 1,
    startX: 0,
    startY: 0,
    originX: 0,
    originY: 0,
    isDragging: false,
    eventListenerAdded: false,
    renderDetailedVerticalSlices: true,
};

window.getCanvasToClientRatio = () => {
    return {x: window.canvas.width / window.canvas.clientWidth, y: window.canvas.height / window.canvas.clientHeight};
}

window.toCanvasCoordinates = (point2d) => window.toCanvasCoordinates(point2d.x, point2d.y);

window.toCanvasCoordinates = (x, y) => {
    let ratio = window.getCanvasToClientRatio();
    const rect = window.canvas.getBoundingClientRect();
    let transformedX = (x * ratio.x - rect.left * ratio.x - window.canvasState.originX) / window.canvasState.scale;
    let transformedY = (y * ratio.y - rect.top * ratio.y - window.canvasState.originY) / window.canvasState.scale;
    return { x: transformedX, y: transformedY };
}


window.addEventListener('resize', () => resizeCanvas());

function resizeCanvas(firstRender) {
    let canvas = document.getElementById(canvasId);

    // Set the canvas width and height to match the display size
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    draw(firstRender, window.canvasState.renderDetailedVerticalSlices);
}


function renderLine(from, to) {
    const ctx = window.canvasContext;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(from.getRightBorderMidpoint().x, from.getRightBorderMidpoint().y);
    ctx.lineTo(to.getLeftBorderMidpoint().x, to.getLeftBorderMidpoint().y);
    ctx.stroke();
}

function draw(reset = false, renderDetailedVerticalSlices = true) {

    let canvas =  window.canvas;
    let ctx = window.canvasContext;

    ctx.setTransform(window.canvasState.scale, 0, 0, window.canvasState.scale, window.canvasState.originX, window.canvasState.originY);
    ctx.clearRect(-window.canvasState.originX / window.canvasState.scale, -window.canvasState.originY / window.canvasState.scale, canvas.width / window.canvasState.scale, canvas.height / window.canvasState.scale);
    ctx.save();

    ctx.strokeStyle = 'black';
    ctx.lineWidth = 1;

    ///////////////////
    let verticalSliceY = reset ? 20 : undefined;
    // Draw nodes
    window.canvasState.verticalSlices.forEach(verticalSlice => {
        if(reset)
        {
            verticalSlice.x = 20;
            verticalSlice.y = verticalSliceY;
        }
        verticalSlice.render(renderDetailedVerticalSlices);
        if(reset) {
            verticalSliceY = verticalSlice.getBottom() + 80;
        }
    });

    window.canvasState.verticalSlices.forEach(verticalSlice => {
        verticalSlice.verticalSliceCalls.forEach(verticalSliceCall => {

            let fromVerticalSlice = verticalSlice;
            let fromNode = verticalSlice.findOutgoingPayloadNode(verticalSliceCall.payloadId);
            let toVerticalSlice = window.canvasState.verticalSlices.find(vs => vs.id === verticalSliceCall.calledVerticalSliceId);
            if (toVerticalSlice === undefined) {
                return;
            }
            let toNode = toVerticalSlice.findIncomingPayloadNode(verticalSliceCall.payloadId);
            if (fromNode && toNode && renderDetailedVerticalSlices) {
                renderQuadraticLine(fromNode, toNode);
            }
            else if (fromVerticalSlice && toVerticalSlice) {
               renderQuadraticLine(fromVerticalSlice, toVerticalSlice);
            }
        });
    });
}

function renderQuadraticLine(from, to) {

    const ctx = window.canvasContext;
    ctx.lineWidth = 5;
    let fromCoords = from.getRightBorderMidpoint();
    let toCoords = to.getLeftBorderMidpoint();
    let diffX = Math.max(fromCoords.x - toCoords.x, 0);
    let diffY = diffX > 0 ? fromCoords.y - toCoords.y : 0;
    let controlPoint1X = fromCoords.x  + diffX/2;
    let controlPoint1Y = fromCoords.y - diffY/2;
    let controlPoint2X = toCoords.x -diffX/2;
    let controlPoint2Y =  toCoords.y +diffY/2;

    ctx.beginPath();
    ctx.moveTo(from.getRightBorderMidpoint().x, from.getRightBorderMidpoint().y);
    ctx.bezierCurveTo(controlPoint1X, controlPoint1Y, controlPoint2X, controlPoint2Y, to.getLeftBorderMidpoint().x, to.getLeftBorderMidpoint().y);
    ctx.stroke();
}

window.setRenderingOptions = (renderingOptions) => {
    window.canvasState.renderDetailedVerticalSlices = renderingOptions.renderDetailedVerticalSlices;
}

window.renderVerticalSlices = (graph, reset) => {

    mapGraphData(graph, reset);
    if(reset === false) {
        window.canvasState.verticalSlices.forEach(verticalSlice => {
            let newVerticalSlice = graph.verticalSlices.find(vs => vs.id === verticalSlice.id);
            if (newVerticalSlice !== undefined) {
                newVerticalSlice.x = verticalSlice.x;
                newVerticalSlice.y = verticalSlice.y;
            }
        });
    }
    window.canvasState.verticalSlices = graph.verticalSlices;

    const zoom = (e) => {
        e.preventDefault();
        let ratio = window.getCanvasToClientRatio();
        const mouseX = e.offsetX * ratio.x;
        const mouseY = e.offsetY * ratio.y;
        const zoomFactor = e.deltaY < 0 ? 1.1 : 0.9;
        window.canvasState.originX = mouseX - (mouseX - window.canvasState.originX) * zoomFactor;
        window.canvasState.originY = mouseY - (mouseY - window.canvasState.originY) * zoomFactor;

        window.canvasState.scale *= zoomFactor;
        draw(undefined, window.canvasState.renderDetailedVerticalSlices);
    };


    const canvasMouseMove = (e) => {
        let draggingVerticalSlice = false;
        window.canvasState.verticalSlices.forEach(verticalSlice => {
        
            verticalSlice.onMouseMove(e);
            if(verticalSlice.lastDragCoords !== undefined)
            {
                draggingVerticalSlice = true;
            }
        });

        let canvasCoords = window.toCanvasCoordinates(e.x, e.y);
        const rect = window.canvas.getBoundingClientRect();

        let valueX = canvasCoords.x + (window.canvasState.originX / window.canvasState.scale);
        console.log("posX: " + canvasCoords.x + " originX: " + window.canvasState.originX + " startX: " + window.canvasState.startX + " scale: " + window.canvasState.scale + " valueX: " + valueX);

        if (window.canvasState.isDragging ) {
            window.canvasState.originX = e.offsetX - window.canvasState.startX;
            window.canvasState.originY = e.offsetY - window.canvasState.startY;
            draw( undefined, window.canvasState.renderDetailedVerticalSlices);
        } else if (window.canvasState.verticalSlices.some(vs => vs.requiresRender)) {
            draw(undefined, window.canvasState.renderDetailedVerticalSlices);
        }
    };

    const canvasMouseLeave = (e) => {
        window.canvasState.isDragging = false;
    }
    const canvasMouseDown = (e) => {

        let handled = false
        window.canvasState.verticalSlices.forEach(verticalSlice => {
            if (!handled) {
                handled = verticalSlice.onMouseDown(e);
            }
        });

        if(!handled) {
            window.canvasState.isDragging = true;
            window.canvasState.startX = e.offsetX - window.canvasState.originX;
            window.canvasState.startY = e.offsetY - window.canvasState.originY;
        }
    }

    const canvasMouseUp = (e) => {
        window.canvasState.isDragging = false;
        window.canvasState.verticalSlices.forEach(verticalSlice => {
            verticalSlice.onMouseUp(e);
        });
    }
    if (window.canvasState.eventListenerAdded === false) {
        let canvas = document.getElementById(canvasId);
        canvas.addEventListener("wheel", zoom);
        canvas.addEventListener("mousemove", canvasMouseMove);
        canvas.addEventListener("mousedown", canvasMouseDown);
        canvas.addEventListener("mouseup", canvasMouseUp);
        canvas.addEventListener("mouseleave", canvasMouseLeave);
        window.canvasState.eventListenerAdded = true;
        window.canvas = canvas;
        window.canvasContext =  canvas.getContext("2d");
    }

    if (reset) {
        resizeCanvas(true);
    } else {
        draw(undefined, window.canvasState.renderDetailedVerticalSlices);
    }
};

function mapGraphData(graphData) {
    graphData.verticalSlices = graphData.verticalSlices.map(verticalSlice => mapVerticalSlice(verticalSlice));
}


function mapVerticalSlice(verticalSlice) {
    let graphVerticalSlice = new GraphVerticalSlice();
    graphVerticalSlice.id = verticalSlice.id;
    graphVerticalSlice.primaryPort = mapNode(verticalSlice.primaryPort);
    graphVerticalSlice.primaryAdapterTuples = verticalSlice.primaryAdapterTuples.map(tuple => mapPrimaryAdapterTuple(tuple));
    graphVerticalSlice.nServiceBusViaOutboxTuples = verticalSlice.nServiceBusViaOutboxTuples.map(tuple => mapNServiceBusViaOutboxTuple(tuple));
    graphVerticalSlice.kafkaViaOutboxTuples = verticalSlice.kafkaViaOutboxTuples.map(tuple => mapKafkaViaOutboxTuple(tuple));
    graphVerticalSlice.nServiceBusPayloads = verticalSlice.nServiceBusPayloads.map(node => mapNode(node));
    graphVerticalSlice.kafkaPayloads = verticalSlice.kafkaPayloads.map(node => mapNode(node));
    graphVerticalSlice.webApiServiceClientCalls = verticalSlice.webApiServiceClientCalls.map(node => mapNode(node));
    graphVerticalSlice.verticalSliceCalls = verticalSlice.verticalSliceCalls.map(call => mapVerticalSliceCall(call));
    return graphVerticalSlice;

}
class GraphData {
    constructor() {
        this.verticalSlices = [];
    }
}

class PrimaryAdapterTuple {
    constructor() {
        this.primaryAdapter = null;
        this.payload = null;
    }
}

class NServiceBusViaOutboxTuple {
    constructor() {
        this.outboxPayload = null;
        this.nServiceBusPayload = null;
    }
}

class KafkaViaOutboxTuple {
    constructor() {
        this.outboxPayload = null;
        this.kafkaPayload = null;
    }
}

class VerticalSliceCall {
    constructor(calledVerticalSliceId, calledPrimeryAdapterId, payloadId) {
        this.calledVerticalSliceId = calledVerticalSliceId;
        this.calledPrimaryAdapterId = calledPrimeryAdapterId;
        this.payloadId = payloadId;
    }
}


function mapKafkaViaOutboxTuple(tuple) {
    let graphTuple = new KafkaViaOutboxTuple();
    graphTuple.outboxPayload = mapNode(tuple.outboxPayload);
    graphTuple.kafkaPayload = mapNode(tuple.kafkaPayload);
    return graphTuple;
}

function mapNServiceBusViaOutboxTuple(tuple) {
    let graphTuple = new PrimaryAdapterTuple();
    graphTuple.outboxPayload = mapNode(tuple.outboxPayload);
    graphTuple.nServiceBusPayload = mapNode(tuple.nServiceBusPayload);
    return graphTuple;
}

function mapPrimaryAdapterTuple(tuple) {
    let graphTuple = new PrimaryAdapterTuple();
    graphTuple.primaryAdapter = mapNode(tuple.primaryAdapter);
    if (tuple.payload != null) {
        graphTuple.payload = mapNode(tuple.payload);
    }
    return graphTuple;
}

function mapVerticalSliceCall(call) {
    return new VerticalSliceCall(call.calledVerticalSliceId, call.calledPrimaryAdapterId, call.payloadId);
}

function mapNode(node) {
    return new VerticalSliceNode(node.id, node.text, node.link, node.nodeTypeId);
}